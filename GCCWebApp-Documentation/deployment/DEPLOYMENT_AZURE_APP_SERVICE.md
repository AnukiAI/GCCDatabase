# Azure App Service Deployment - Fast Track Guide

**Date:** October 10, 2025
**Reason:** AccuWebHosting/Plesk causing weeks of delays - migrating to Azure
**Goal:** Get GCCWebApp.com live TODAY

---

## Why Azure App Service?

**vs AccuWebHosting/Plesk:**
- No web.config wrestling
- No environment variable hunting through Plesk panels
- No "contact support to install hosting bundle"
- Deploy in minutes, not weeks
- Native ASP.NET Core 8.0 support
- Built-in SSL, scaling, monitoring

---

## Prerequisites

1. **Azure Account**
   - Sign up at https://portal.azure.com (free tier available)
   - Credit card required but you get $200 free credits for 30 days

2. **Azure CLI** (recommended) or Azure Portal
   - Download: https://aka.ms/installazurecliwindows
   - Or use Cloud Shell in Azure Portal (no install needed)

3. **Your Current Assets**
   - ✅ Application already built (publish-output folder ready)
   - ✅ SQL Server database at 68.71.129.120,1533
   - ✅ Production config (appsettings.Production.json)
   - ✅ Domain: gccwebapp.com (you'll point DNS to Azure)

---

## OPTION 1: Keep Your Existing SQL Server (FASTEST)

**Pros:** No data migration, keep AccuWeb database, deploy app only
**Cons:** Split hosting (app on Azure, DB on AccuWeb)
**Time:** 15 minutes

## OPTION 2: Move Everything to Azure

**Pros:** Everything in one place, better performance, easier management
**Cons:** Need to migrate database (~30 minutes extra)
**Time:** 45 minutes

**RECOMMENDATION:** Start with Option 1 (keep existing database) to get live FAST. Migrate database to Azure SQL later if desired.

---

# FAST TRACK: Option 1 - Azure App Service + Keep Existing Database

## Step 1: Install Azure CLI (5 minutes)

**On Windows:**

```powershell
# Download and run installer
# https://aka.ms/installazurecliwindows

# Or use winget
winget install -e --id Microsoft.AzureCLI

# After install, restart terminal and verify
az --version
```

**Alternative:** Use Azure Cloud Shell (no install) at https://shell.azure.com

---

## Step 2: Login to Azure (1 minute)

```bash
# Login to your Azure account
az login

# This will open browser - login with your Microsoft account
# After login, close browser and return to terminal

# Verify you're logged in
az account show
```

---

## Step 3: Create Azure Resources (5 minutes)

**Run these commands in PowerShell/Terminal:**

```bash
# Set variables (CHANGE THESE if desired)
$resourceGroup = "GCCWebApp-RG"
$location = "eastus"
$appServicePlan = "GCCWebApp-Plan"
$webAppName = "gccwebapp-production"  # Must be globally unique
$sku = "B1"  # Basic tier - $13/month. Use "F1" for free tier (testing only)

# Create Resource Group
az group create --name $resourceGroup --location $location

# Create App Service Plan
az appservice plan create `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --location $location `
  --sku $sku `
  --is-linux false

# Create Web App
az webapp create `
  --name $webAppName `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --runtime "DOTNET|8.0"
```

**Expected Output:**
```
✅ Resource group created
✅ App Service Plan created (B1 tier - $13/month)
✅ Web App created: https://gccwebapp-production.azurewebsites.net
```

---

## Step 4: Configure Environment Variables (2 minutes)

**Set Application Settings (these replace web.config environment variables):**

```bash
# Set ASPNETCORE_ENVIRONMENT to Production
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings ASPNETCORE_ENVIRONMENT=Production

# Set connection string (using your EXISTING AccuWeb SQL Server)
az webapp config connection-string set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --connection-string-type SQLServer `
  --settings DefaultConnection="Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"

# Set JWT Secret (from your appsettings.Production.json)
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings GCC_JWT_SECRET="5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ=="

# Enable detailed logging (temporary, to troubleshoot)
az webapp log config `
  --name $webAppName `
  --resource-group $resourceGroup `
  --application-logging filesystem `
  --detailed-error-messages true `
  --failed-request-tracing true `
  --web-server-logging filesystem
```

---

## Step 5: Deploy Your Application (3 minutes)

### Method A: Deploy from publish folder (RECOMMENDED for first deployment)

```bash
# Navigate to your project directory
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Publish the app to a folder
dotnet publish -c Release -o ./publish

# Create a ZIP file (Azure requires ZIP deployment)
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Deploy to Azure
az webapp deploy `
  --name $webAppName `
  --resource-group $resourceGroup `
  --src-path ./publish.zip `
  --type zip
```

### Method B: Deploy directly from Visual Studio

1. Open your project in Visual Studio
2. Right-click project → **Publish**
3. Choose **Azure** → **Azure App Service (Windows)**
4. Select your subscription
5. Select your Web App: `gccwebapp-production`
6. Click **Publish**

---

## Step 6: Configure Database Firewall (CRITICAL!)

**Your AccuWeb SQL Server needs to allow Azure IPs to connect:**

### Option A: Allow All Azure IPs (simplest)

Contact AccuWebHosting support:
```
Subject: Allow Azure App Service IP Addresses to SQL Server

Body:
Please add the following IP ranges to the SQL Server firewall at 68.71.129.120,1533:
- Allow Azure Services: 0.0.0.0/0 (or provide specific Azure East US IP ranges)
- Database: gccwebap_gccdb
- Port: 1533

This is required for our Azure-hosted web application to connect.
```

### Option B: Get Your Azure App Outbound IPs (more secure)

```bash
# Get outbound IPs for your Azure App
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query outboundIpAddresses `
  --output tsv
```

Send these specific IPs to AccuWebHosting to whitelist.

---

## Step 7: Verify Deployment (2 minutes)

### Test the Azure Default URL First

```bash
# Get your Azure URL
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query defaultHostName `
  --output tsv
```

**Open in browser:**
- https://gccwebapp-production.azurewebsites.net
- https://gccwebapp-production.azurewebsites.net/health
- https://gccwebapp-production.azurewebsites.net/Certificate/CertificateTileView

**Expected Results:**
- ✅ App loads (no HTTP 500 error!)
- ✅ Health endpoint returns "Healthy"
- ✅ Certificate tile view loads

**If you get database connection errors:**
- Check firewall settings (Step 6)
- Verify connection string is correct
- Check logs (see Step 9)

---

## Step 8: Configure Custom Domain (5 minutes)

**Once Azure app is working, point gccwebapp.com to Azure:**

### Add Custom Domain to Azure

```bash
# Add your custom domain
az webapp config hostname add `
  --webapp-name $webAppName `
  --resource-group $resourceGroup `
  --hostname gccwebapp.com

az webapp config hostname add `
  --webapp-name $webAppName `
  --resource-group $resourceGroup `
  --hostname www.gccwebapp.com
```

### Update DNS Records

**In your domain registrar (where you bought gccwebapp.com):**

1. **Add CNAME record:**
   - Type: CNAME
   - Name: www
   - Value: gccwebapp-production.azurewebsites.net
   - TTL: 3600

2. **Add A record or ALIAS for root domain:**
   - Type: A (or ALIAS if supported)
   - Name: @ (or leave blank for root)
   - Value: Get from Azure:
     ```bash
     az webapp show --name $webAppName --resource-group $resourceGroup --query inboundIpAddress --output tsv
     ```
   - TTL: 3600

### Enable HTTPS (Free SSL)

```bash
# Bind SSL certificate (Azure provides FREE Let's Encrypt certificate)
az webapp config ssl bind `
  --name $webAppName `
  --resource-group $resourceGroup `
  --certificate-thumbprint auto `
  --ssl-type SNI
```

**Wait 5-10 minutes for DNS propagation, then:**
- Visit https://gccwebapp.com
- Should redirect to HTTPS automatically
- Free SSL certificate automatically renews

---

## Step 9: Check Logs (if issues occur)

### View Application Logs

```bash
# Stream logs in real-time
az webapp log tail --name $webAppName --resource-group $resourceGroup

# Or download logs
az webapp log download --name $webAppName --resource-group $resourceGroup --log-file logs.zip
```

### View Logs in Azure Portal

1. Go to https://portal.azure.com
2. Navigate to your Web App: `gccwebapp-production`
3. Click **Log stream** (left sidebar)
4. Watch real-time logs

---

## Step 10: Post-Deployment Configuration

### Update appsettings.Production.json (if needed)

**Azure App Settings OVERRIDE appsettings.Production.json, so:**
- Database connection string is in Azure (secure)
- JWT secret is in Azure (secure)
- Environment variable is in Azure

**You can still deploy appsettings.Production.json for other settings like:**
- CORS origins
- Rate limiting
- Password policies
- Feature flags

### Enable Always On (prevents app from sleeping)

```bash
az webapp config set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --always-on true
```

### Set Up Deployment Slots (optional - for staging)

```bash
# Create staging slot
az webapp deployment slot create `
  --name $webAppName `
  --resource-group $resourceGroup `
  --slot staging

# Deploy to staging first, test, then swap to production
```

---

## Monitoring & Troubleshooting

### Enable Application Insights (optional but recommended)

```bash
# Create Application Insights
az monitor app-insights component create `
  --app gccwebapp-insights `
  --location $location `
  --resource-group $resourceGroup `
  --application-type web

# Link to Web App
$instrumentationKey = az monitor app-insights component show `
  --app gccwebapp-insights `
  --resource-group $resourceGroup `
  --query instrumentationKey `
  --output tsv

az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$instrumentationKey"
```

### Common Issues

**Issue 1: "Cannot connect to database"**

Solution:
- Check firewall allows Azure IPs (Step 6)
- Verify connection string is correct
- Test connection from Azure Cloud Shell:
  ```bash
  # Install sqlcmd in Cloud Shell
  sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
  ```

**Issue 2: "Application Error 500.30"**

Solution:
- Check logs: `az webapp log tail`
- Verify ASPNETCORE_ENVIRONMENT is set
- Verify runtime is DOTNET 8.0

**Issue 3: Static files (CSS/JS) not loading**

Solution:
- Azure App Service handles wwwroot automatically
- Check that wwwroot folder is included in publish
- Check browser console for 404 errors

---

## Cost Breakdown

**Option 1: B1 App Service Plan (Basic)**
- App Service: $13/month
- SSL Certificate: FREE (Let's Encrypt)
- Total: ~$13/month (+ existing AccuWeb DB cost)

**Option 2: Free Tier (F1) - Testing Only**
- App Service: FREE
- Limitations: No custom domain SSL, app sleeps after 20 min idle
- Good for testing, NOT for production

**Option 3: Move to Azure SQL Database (complete migration)**
- App Service (B1): $13/month
- Azure SQL Basic: $5/month
- Total: ~$18/month (cancel AccuWeb)

---

## Deployment Summary

**What You Get:**
- ✅ Fast deployment (15 minutes vs weeks with AccuWeb)
- ✅ No web.config wrestling
- ✅ No Plesk panel hunting
- ✅ Native ASP.NET Core 8.0 support
- ✅ Free SSL certificate (auto-renews)
- ✅ Scalable (upgrade to higher tier anytime)
- ✅ Built-in monitoring and logs
- ✅ Deployment slots for staging/production

**What You Keep:**
- ✅ Your existing SQL Server database (no migration needed)
- ✅ Your production config and data
- ✅ Your domain (gccwebapp.com)

---

## Quick Command Reference

```bash
# Deploy updated code
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az webapp deploy --name gccwebapp-production --resource-group GCCWebApp-RG --src-path ./publish.zip --type zip

# Restart app
az webapp restart --name gccwebapp-production --resource-group GCCWebApp-RG

# View logs
az webapp log tail --name gccwebapp-production --resource-group GCCWebApp-RG

# Update settings
az webapp config appsettings set --name gccwebapp-production --resource-group GCCWebApp-RG --settings KEY=VALUE

# Stop app (for maintenance)
az webapp stop --name gccwebapp-production --resource-group GCCWebApp-RG

# Start app
az webapp start --name gccwebapp-production --resource-group GCCWebApp-RG
```

---

## Migration from AccuWeb - Complete Checklist

**Before You Start:**
- [ ] Azure account created
- [ ] Azure CLI installed (or using Cloud Shell)
- [ ] Logged in: `az login`

**Deploy to Azure:**
- [ ] Create resource group and App Service (Step 3)
- [ ] Configure environment variables (Step 4)
- [ ] Deploy application (Step 5)
- [ ] Configure database firewall to allow Azure (Step 6)
- [ ] Test Azure URL: https://gccwebapp-production.azurewebsites.net

**Configure Domain:**
- [ ] Add custom domain in Azure (Step 8)
- [ ] Update DNS records (CNAME and A record)
- [ ] Enable HTTPS/SSL (free certificate)
- [ ] Test custom domain: https://gccwebapp.com

**Post-Deployment:**
- [ ] Change default admin passwords
- [ ] Test all key features (login, certificates, admin panel)
- [ ] Enable Application Insights (monitoring)
- [ ] Set up staging slot (optional)
- [ ] Cancel AccuWebHosting service (once Azure is stable)

---

## Support

**Azure Documentation:**
- App Service: https://docs.microsoft.com/azure/app-service/
- Deployment: https://docs.microsoft.com/azure/app-service/deploy-zip
- Custom Domains: https://docs.microsoft.com/azure/app-service/app-service-web-tutorial-custom-domain

**Azure Support:**
- Free tier: Community support (Stack Overflow, forums)
- Paid plans: 24/7 support with SLA

**Cost vs AccuWebHosting:**
- AccuWebHosting: $?/month (but weeks of delays)
- Azure B1: $13/month (deploy in 15 minutes)
- **Value:** Priceless (no more weeks lost to Plesk issues)

---

**Generated:** October 10, 2025
**Status:** Ready to Deploy
**Expected Time to Live:** 15 minutes

**NEXT STEP:** Open PowerShell, run Step 3 commands, and get live TODAY.

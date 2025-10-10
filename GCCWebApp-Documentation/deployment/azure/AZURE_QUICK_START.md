# Azure App Service - 15-Minute Quick Start

**Goal:** Get gccwebapp.com live on Azure App Service in 15 minutes

---

## Prerequisites Check

- [ ] Azure account (https://portal.azure.com) - Free trial OK
- [ ] Azure CLI installed OR use Cloud Shell (https://shell.azure.com)
- [ ] Your app is already built and tested locally

---

## Step-by-Step Commands

### 1. Login to Azure (1 min)

```bash
az login
```

Browser will open → Login → Return to terminal

---

### 2. Set Variables (30 seconds)

**Copy/paste this block (adjust $webAppName if needed):**

```powershell
$resourceGroup = "GCCWebApp-RG"
$location = "eastus"
$appServicePlan = "GCCWebApp-Plan"
$webAppName = "gccwebapp-prod"  # Must be globally unique - change if taken
$sku = "B1"  # $13/month - use "F1" for free (testing only)
```

---

### 3. Create Azure Resources (3 min)

```bash
# Create resource group
az group create --name $resourceGroup --location $location

# Create app service plan
az appservice plan create --name $appServicePlan --resource-group $resourceGroup --location $location --sku $sku --is-linux false

# Create web app
az webapp create --name $webAppName --resource-group $resourceGroup --plan $appServicePlan --runtime "DOTNET|8.0"
```

**Wait for:** "App service plan created", "Web app created"

---

### 4. Configure App Settings (2 min)

```bash
# Set environment
az webapp config appsettings set --name $webAppName --resource-group $resourceGroup --settings ASPNETCORE_ENVIRONMENT=Production

# Set database connection (using your EXISTING AccuWeb database)
az webapp config connection-string set --name $webAppName --resource-group $resourceGroup --connection-string-type SQLServer --settings DefaultConnection="Server=68.71.129.120,1533;Database=gccwebap_gccdb;User Id=adminGCC;Password=Q3Ez4elgE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"

# Set JWT secret
az webapp config appsettings set --name $webAppName --resource-group $resourceGroup --settings GCC_JWT_SECRET="5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ=="

# Enable logging
az webapp log config --name $webAppName --resource-group $resourceGroup --application-logging filesystem --detailed-error-messages true --web-server-logging filesystem
```

---

### 5. Deploy Your App (3 min)

```bash
# Navigate to project
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Publish
dotnet publish -c Release -o ./publish

# Create ZIP
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Deploy to Azure
az webapp deploy --name $webAppName --resource-group $resourceGroup --src-path ./publish.zip --type zip
```

**Wait for:** "Deployment successful"

---

### 6. Get Outbound IPs for Database Firewall (1 min)

```bash
# Get your app's outbound IPs
az webapp show --name $webAppName --resource-group $resourceGroup --query outboundIpAddresses --output tsv
```

**Copy these IPs** and send to AccuWebHosting support:

```
Subject: Add Azure IPs to SQL Server Firewall

Please whitelist these IPs for SQL Server 68.71.129.120,1533:
[PASTE IPs HERE]

Database: gccwebap_gccdb
Port: 1533
```

---

### 7. Test Your App (1 min)

```bash
# Get your Azure URL
az webapp show --name $webAppName --resource-group $resourceGroup --query defaultHostName --output tsv
```

**Open in browser:**
- https://[your-app-name].azurewebsites.net
- https://[your-app-name].azurewebsites.net/health
- https://[your-app-name].azurewebsites.net/Certificate/CertificateTileView

**Expected:** App loads without HTTP 500 error!

---

### 8. Configure Custom Domain (4 min)

**After AccuWebHosting whitelists Azure IPs (Step 6):**

```bash
# Add custom domain
az webapp config hostname add --webapp-name $webAppName --resource-group $resourceGroup --hostname gccwebapp.com
az webapp config hostname add --webapp-name $webAppName --resource-group $resourceGroup --hostname www.gccwebapp.com

# Get IP for DNS
az webapp show --name $webAppName --resource-group $resourceGroup --query inboundIpAddress --output tsv
```

**Update DNS (at your domain registrar):**

1. CNAME record:
   - Name: www
   - Value: [your-app-name].azurewebsites.net

2. A record:
   - Name: @ (or blank)
   - Value: [IP from command above]

**Wait 5-10 minutes for DNS propagation**

```bash
# Enable FREE SSL
az webapp config ssl bind --name $webAppName --resource-group $resourceGroup --certificate-thumbprint auto --ssl-type SNI
```

**Test:** https://gccwebapp.com

---

## If Something Goes Wrong

### View Logs

```bash
# Real-time logs
az webapp log tail --name $webAppName --resource-group $resourceGroup
```

### Restart App

```bash
az webapp restart --name $webAppName --resource-group $resourceGroup
```

### Can't connect to database?

1. Check if AccuWebHosting whitelisted Azure IPs (Step 6)
2. Check connection string is correct (Step 4)
3. Test from Azure Cloud Shell:
   ```bash
   # Install sqlcmd if not available
   sqlcmd -S 68.71.129.120,1533 -d gccwebap_gccdb -U adminGCC -P Q3Ez4elgE_47KNi -Q "SELECT 1"
   ```

---

## Cost

**B1 Plan:** $13/month
- 1.75 GB RAM
- 100 GB storage
- Custom domain + FREE SSL
- Always on (no sleep)
- Good for production

**F1 Plan (Free):** $0/month
- Testing only
- No custom domain SSL
- App sleeps after 20 min idle

---

## Quick Command Reference

```bash
# Redeploy after code changes
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az webapp deploy --name $webAppName --resource-group $resourceGroup --src-path ./publish.zip --type zip

# Restart
az webapp restart --name $webAppName --resource-group $resourceGroup

# View logs
az webapp log tail --name $webAppName --resource-group $resourceGroup

# Stop app
az webapp stop --name $webAppName --resource-group $resourceGroup

# Start app
az webapp start --name $webAppName --resource-group $resourceGroup
```

---

## Success Checklist

After completing all steps:

- [ ] App loads at https://[your-app-name].azurewebsites.net
- [ ] Database connects (certificates load, login works)
- [ ] Custom domain works: https://gccwebapp.com
- [ ] SSL certificate is active (green padlock)
- [ ] Health check works: https://gccwebapp.com/health
- [ ] Admin login works
- [ ] Can create/edit certificates

**You're live!** Cancel AccuWebHosting after verifying everything works.

---

## What You Just Avoided

**With AccuWebHosting/Plesk:**
- ❌ Weeks of troubleshooting
- ❌ Web.config wrestling
- ❌ Environment variable hunting
- ❌ Support tickets for hosting bundle
- ❌ HTTP 500 errors with no logs

**With Azure App Service:**
- ✅ Deployed in 15 minutes
- ✅ Native .NET support
- ✅ Clear error logs
- ✅ One-command deploys
- ✅ Free SSL

---

**Generated:** October 10, 2025
**Time to Deploy:** 15 minutes
**Cost:** $13/month (B1) or $0 (F1 free tier for testing)

**NEXT:** Open PowerShell and start with Step 1!

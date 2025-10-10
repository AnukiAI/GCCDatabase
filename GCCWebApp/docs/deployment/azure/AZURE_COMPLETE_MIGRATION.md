# Complete Migration to Azure - App + Database

**Goal:** Move EVERYTHING from AccuWebHosting to Azure App Service + Azure SQL Database
**Time:** 45 minutes
**Cost:** ~$18/month (App Service B1 + Azure SQL Basic)
**Result:** Complete independence from AccuWebHosting

---

## What We're Migrating

**From AccuWebHosting:**
- SQL Server at 68.71.129.120,1533
- Database: gccwebap_gccdb
- ASP.NET Core app (currently failing on Plesk)

**To Azure:**
- Azure SQL Database (Basic tier - $5/month)
- Azure App Service (B1 tier - $13/month)
- Total: ~$18/month, everything in one place

---

## Prerequisites

- [ ] Azure account (https://portal.azure.com) - Free trial OK ($200 credit)
- [ ] Azure CLI installed OR use Cloud Shell (https://shell.azure.com)
- [ ] SQL Server Management Studio (SSMS) OR Azure Data Studio
- [ ] Your app builds and runs locally

---

## PART 1: Create Azure Resources (5 minutes)

### Step 1: Login to Azure

```bash
az login
```

Browser opens → Login → Return to terminal

---

### Step 2: Set Variables

```powershell
# Azure resource names (change if desired)
$resourceGroup = "GCCWebApp-RG"
$location = "eastus"
$appServicePlan = "GCCWebApp-Plan"
$webAppName = "gccwebapp-prod"  # Must be globally unique
$sqlServerName = "gccwebapp-sql-server"  # Must be globally unique
$sqlDatabaseName = "gccwebapp-db"
$sqlAdminUser = "gccadmin"
$sqlAdminPassword = "GCC#Prod#2025!Azure"  # CHANGE THIS - must be complex

# Tier settings
$appServiceSku = "B1"  # $13/month
$sqlDatabaseSku = "Basic"  # $5/month
```

---

### Step 3: Create Resource Group

```bash
az group create --name $resourceGroup --location $location
```

**Expected:** ✅ Resource group created

---

### Step 4: Create App Service Plan

```bash
az appservice plan create `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --location $location `
  --sku $appServiceSku `
  --is-linux false
```

**Expected:** ✅ App Service Plan created (B1 tier)

---

### Step 5: Create Web App

```bash
az webapp create `
  --name $webAppName `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --runtime "DOTNET|8.0"
```

**Expected:** ✅ Web App created at https://[your-app-name].azurewebsites.net

---

### Step 6: Create Azure SQL Server

```bash
az sql server create `
  --name $sqlServerName `
  --resource-group $resourceGroup `
  --location $location `
  --admin-user $sqlAdminUser `
  --admin-password $sqlAdminPassword
```

**Expected:** ✅ SQL Server created

---

### Step 7: Configure SQL Server Firewall

```bash
# Allow Azure services to access SQL Server
az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServerName `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Allow YOUR IP (for database migration from your computer)
# Get your public IP: https://whatismyipaddress.com/
$myPublicIP = "YOUR_PUBLIC_IP_HERE"  # Replace with your actual IP

az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServerName `
  --name AllowMyIP `
  --start-ip-address $myPublicIP `
  --end-ip-address $myPublicIP
```

**Expected:** ✅ Firewall rules created

---

### Step 8: Create Azure SQL Database

```bash
az sql db create `
  --resource-group $resourceGroup `
  --server $sqlServerName `
  --name $sqlDatabaseName `
  --service-objective Basic `
  --backup-storage-redundancy Local
```

**Expected:** ✅ Database created (this takes 2-3 minutes)

---

## PART 2: Migrate Database (15 minutes)

### Option A: Export/Import via BACPAC (RECOMMENDED - Simplest)

#### Step 1: Export from AccuWebHosting SQL Server

**Using SQL Server Management Studio (SSMS):**

1. **Connect to AccuWebHosting SQL Server:**
   - Server: 68.71.129.120,1533
   - Authentication: SQL Server Authentication
   - Username: adminGCC
   - Password: Q3Ez4elgE_47KNi
   - Database: gccwebap_gccdb

2. **Export Database:**
   - Right-click on database `gccwebap_gccdb`
   - Tasks → Export Data-tier Application...
   - Click Next
   - Save to file: `C:\Temp\gccwebapp.bacpac`
   - Click Next → Next → Finish
   - **Wait:** 5-10 minutes (depends on database size)

**Using Azure Data Studio (Alternative):**

1. Install SQL Server dacpac extension
2. Connect to 68.71.129.120,1533
3. Right-click database → Data-tier Application Wizard → Export
4. Save to `C:\Temp\gccwebapp.bacpac`

**Using Command Line (Alternative):**

```bash
# Install SqlPackage if not installed
# Download from: https://aka.ms/sqlpackage-windows

SqlPackage.exe /Action:Export `
  /SourceServerName:"68.71.129.120,1533" `
  /SourceDatabaseName:"gccwebap_gccdb" `
  /SourceUser:"adminGCC" `
  /SourcePassword:"Q3Ez4elgE_47KNi" `
  /TargetFile:"C:\Temp\gccwebapp.bacpac"
```

---

#### Step 2: Get Azure SQL Connection Details

```bash
# Get SQL Server fully qualified name
az sql server show `
  --name $sqlServerName `
  --resource-group $resourceGroup `
  --query fullyQualifiedDomainName `
  --output tsv
```

**Save this value** - it will be something like: `gccwebapp-sql-server.database.windows.net`

---

#### Step 3: Import to Azure SQL Database

**Using SSMS:**

1. **Connect to Azure SQL Server:**
   - Server: [name from Step 2].database.windows.net
   - Authentication: SQL Server Authentication
   - Username: gccadmin
   - Password: [your password from Part 1, Step 2]

2. **Import Database:**
   - Right-click on Databases
   - Import Data-tier Application...
   - Click Next
   - Select file: `C:\Temp\gccwebapp.bacpac`
   - Database name: gccwebapp-db
   - Service Tier: Basic
   - Click Next → Next → Finish
   - **Wait:** 5-10 minutes

**Using Command Line:**

```bash
# Get connection details
$sqlServerFqdn = az sql server show --name $sqlServerName --resource-group $resourceGroup --query fullyQualifiedDomainName --output tsv

# Import BACPAC
SqlPackage.exe /Action:Import `
  /SourceFile:"C:\Temp\gccwebapp.bacpac" `
  /TargetServerName:"$sqlServerFqdn" `
  /TargetDatabaseName:"gccwebapp-db" `
  /TargetUser:"gccadmin" `
  /TargetPassword:"GCC#Prod#2025!Azure"
```

**Using Azure Portal (Alternative):**

1. Go to https://portal.azure.com
2. Navigate to your SQL Database: gccwebapp-db
3. Click "Import database"
4. Upload the .bacpac file
5. Wait for import to complete

---

### Option B: Direct Database Copy (Faster if large database)

**Use Azure Database Migration Service (DMS):**

This is better for large databases (>5GB) but requires more setup.
For your database size, Option A (BACPAC) should work fine.

---

## PART 3: Verify Database Migration (5 minutes)

### Step 1: Connect to Azure SQL Database

**Using SSMS or Azure Data Studio:**

1. Server: [your-sql-server].database.windows.net
2. Database: gccwebapp-db
3. Username: gccadmin
4. Password: [your password]

---

### Step 2: Verify Data

**Run these queries to verify migration:**

```sql
-- Check table count
SELECT COUNT(*) AS TableCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';

-- Check certificate count
SELECT COUNT(*) AS CertificateCount FROM Certificates;

-- Check user count
SELECT COUNT(*) AS UserCount FROM Users;

-- Check product IDs
SELECT COUNT(*) AS ProductIdCount FROM CertificateProductIds;

-- Verify admin user exists
SELECT Id, Email, UserName FROM Users WHERE Email = 'admin@greencirclecertified.com';
```

**Expected Results:**
- ✅ All tables exist (Certificates, Users, Companies, Products, etc.)
- ✅ Data counts match your source database
- ✅ Admin user exists

---

## PART 4: Configure App Service (5 minutes)

### Step 1: Get Azure SQL Connection String

```bash
# Build connection string
$sqlServerFqdn = az sql server show --name $sqlServerName --resource-group $resourceGroup --query fullyQualifiedDomainName --output tsv

$connectionString = "Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminUser};Password=${sqlAdminPassword};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "Connection String: $connectionString"
```

---

### Step 2: Configure App Settings

```bash
# Set environment
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings ASPNETCORE_ENVIRONMENT=Production

# Set connection string (use the one from Step 1)
az webapp config connection-string set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --connection-string-type SQLServer `
  --settings DefaultConnection="$connectionString"

# Set JWT secret (from your appsettings.Production.json)
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings GCC_JWT_SECRET="5T8hr4WpKKmqqDIHBzFW7OzfYdStIzuvKEZJYwQaSkfe5ADFryPGJHGAuxPCECnuo09ZI8qiLqURyVT1DR1NtQ=="

# Enable detailed logging (temporary)
az webapp log config `
  --name $webAppName `
  --resource-group $resourceGroup `
  --application-logging filesystem `
  --detailed-error-messages true `
  --failed-request-tracing true `
  --web-server-logging filesystem

# Enable Always On (prevent app from sleeping)
az webapp config set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --always-on true
```

---

## PART 5: Deploy Application (5 minutes)

### Step 1: Publish Application

```bash
# Navigate to project
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Clean and publish
dotnet clean
dotnet publish -c Release -o ./publish
```

---

### Step 2: Create Deployment Package

```bash
# Create ZIP file
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
```

---

### Step 3: Deploy to Azure

```bash
# Deploy
az webapp deploy `
  --name $webAppName `
  --resource-group $resourceGroup `
  --src-path ./publish.zip `
  --type zip `
  --timeout 300
```

**Wait for:** "Deployment successful" (2-3 minutes)

---

## PART 6: Verify Deployment (5 minutes)

### Step 1: Get Your App URL

```bash
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query defaultHostName `
  --output tsv
```

**Copy this URL** - it will be: https://[your-app-name].azurewebsites.net

---

### Step 2: Test Application

**Open in browser:**

1. **Home/Health:**
   - https://[your-app-name].azurewebsites.net/health
   - **Expected:** "Healthy"

2. **Certificate Tile View:**
   - https://[your-app-name].azurewebsites.net/Certificate/CertificateTileView
   - **Expected:** Certificates load (proves database connection works)

3. **Login:**
   - https://[your-app-name].azurewebsites.net/Account/Login
   - Email: admin@greencirclecertified.com
   - Password: Admin123!
   - **Expected:** Login successful, redirected to dashboard

4. **Admin Panel:**
   - https://[your-app-name].azurewebsites.net/Admin/UserManagement
   - **Expected:** User list loads

---

### Step 3: Check Logs (if issues)

```bash
# Stream logs in real-time
az webapp log tail --name $webAppName --resource-group $resourceGroup
```

**Look for:**
- ✅ "Application started. Press Ctrl+C to shut down."
- ✅ "Now listening on: http://[::]:8080"
- ❌ Any errors or exceptions

---

## PART 7: Configure Custom Domain (5 minutes)

### Step 1: Add Custom Domain to Azure

```bash
# Add gccwebapp.com
az webapp config hostname add `
  --webapp-name $webAppName `
  --resource-group $resourceGroup `
  --hostname gccwebapp.com

# Add www.gccwebapp.com
az webapp config hostname add `
  --webapp-name $webAppName `
  --resource-group $resourceGroup `
  --hostname www.gccwebapp.com
```

---

### Step 2: Get DNS Configuration Values

```bash
# Get IP address for A record
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query inboundIpAddress `
  --output tsv

# Get hostname for CNAME verification
az webapp show `
  --name $webAppName `
  --resource-group $resourceGroup `
  --query defaultHostName `
  --output tsv
```

---

### Step 3: Update DNS Records

**At your domain registrar (where you bought gccwebapp.com):**

**Add these DNS records:**

1. **A Record (for root domain):**
   - Type: A
   - Name: @ (or leave blank)
   - Value: [IP from command above]
   - TTL: 3600

2. **CNAME Record (for www):**
   - Type: CNAME
   - Name: www
   - Value: [hostname].azurewebsites.net
   - TTL: 3600

3. **TXT Record (for domain verification):**
   - Type: TXT
   - Name: asuid
   - Value: Get from Azure Portal → Your Web App → Custom domains → Custom Domain Verification ID
   - TTL: 3600

**Save changes and wait 5-10 minutes for DNS propagation**

---

### Step 4: Enable HTTPS/SSL

```bash
# Create managed certificate (FREE - Let's Encrypt)
az webapp config ssl create `
  --name $webAppName `
  --resource-group $resourceGroup `
  --hostname gccwebapp.com

az webapp config ssl create `
  --name $webAppName `
  --resource-group $resourceGroup `
  --hostname www.gccwebapp.com

# Bind certificates
az webapp config ssl bind `
  --name $webAppName `
  --resource-group $resourceGroup `
  --certificate-thumbprint auto `
  --ssl-type SNI
```

**Note:** Azure provides FREE SSL certificates that auto-renew!

---

## PART 8: Post-Migration Tasks (5 minutes)

### Step 1: Change Default Passwords

**Login to your app and change these immediately:**

1. Admin: admin@greencirclecertified.com / Admin123!
2. Analyst: analyst@greencirclecertified.com / Analyst123!
3. External: partner@amazon.com / Partner123!

---

### Step 2: Verify All Features

**Test checklist:**

- [ ] Login/logout works
- [ ] Certificate tile view loads and filters
- [ ] Autocomplete search works
- [ ] Certificate details page loads
- [ ] Create new certificate (Admin/Analyst)
- [ ] Edit certificate
- [ ] Upload files
- [ ] Export certificates (CSV/Excel)
- [ ] Admin panel (User Management, Role Management)
- [ ] API endpoints work (test with Swagger if enabled)

---

### Step 3: Set Up Monitoring (Optional but Recommended)

```bash
# Create Application Insights
az monitor app-insights component create `
  --app gccwebapp-insights `
  --location $location `
  --resource-group $resourceGroup `
  --application-type web

# Get instrumentation key
$instrumentationKey = az monitor app-insights component show `
  --app gccwebapp-insights `
  --resource-group $resourceGroup `
  --query instrumentationKey `
  --output tsv

# Link to Web App
az webapp config appsettings set `
  --name $webAppName `
  --resource-group $resourceGroup `
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$instrumentationKey"
```

**This gives you:**
- Request tracking
- Performance monitoring
- Error tracking
- Usage analytics

---

### Step 4: Configure Backups (Recommended)

```bash
# Enable automated backups (requires Standard tier or higher)
# If you're on B1, this requires upgrade to S1 ($75/month)
# For Basic tier, use manual backups instead

# Manual database backup to Azure Blob Storage (recommended for Basic tier)
# This is done through Azure Portal or scheduled scripts
```

**For now (Basic tier):**
- Azure SQL Basic tier has built-in geo-redundant backups (7 days retention)
- You're already protected without extra cost

---

## PART 9: Cleanup & Cancel AccuWebHosting

### Step 1: Verify Everything Works on Azure

**Checklist before canceling AccuWebHosting:**

- [ ] App works at https://gccwebapp.com
- [ ] All features tested and working
- [ ] Custom domain and SSL working
- [ ] Database fully migrated and verified
- [ ] Admin access working
- [ ] No critical errors in logs

---

### Step 2: Cancel AccuWebHosting Services

**Once Azure is stable for 24-48 hours:**

1. **Backup your AccuWebHosting data** (just in case)
2. **Download any files** you might need
3. **Cancel the hosting plan**
4. **Cancel the database plan**

**DO NOT cancel until Azure is proven stable!**

---

## Cost Breakdown

### Azure Monthly Costs

**App Service B1:**
- Cost: ~$13/month
- 1.75 GB RAM
- 100 GB storage
- Custom domain + SSL included

**Azure SQL Basic:**
- Cost: ~$5/month
- 2 GB storage
- Good for small-medium apps
- Built-in backups (7 days)

**Total: ~$18/month**

### Cost Comparison

**AccuWebHosting:** $?? + weeks of headaches
**Azure:** $18/month + deploy in 45 minutes

**Upgrade Paths (if needed later):**
- App Service S1: $75/month (backups, staging slots, auto-scale)
- Azure SQL Standard S0: $15/month (10 GB, better performance)

---

## Troubleshooting

### Issue 1: "Cannot connect to database"

**Check:**
1. Firewall rules allow Azure services (Part 1, Step 7)
2. Connection string is correct (Part 4, Step 2)
3. Database exists and has data (Part 3)

**Test connection:**
```bash
az webapp log tail --name $webAppName --resource-group $resourceGroup
```
Look for "SqlException" or connection errors

---

### Issue 2: "Application Error 500.30"

**Check:**
1. ASPNETCORE_ENVIRONMENT is set to Production
2. Runtime is DOTNET 8.0
3. Check logs for startup errors

**Fix:**
```bash
az webapp config show --name $webAppName --resource-group $resourceGroup --query linuxFxVersion
# Should show: "DOTNET|8.0"
```

---

### Issue 3: Static files not loading

**Check:**
1. wwwroot folder is included in publish
2. No 404 errors in browser console
3. Azure App Service handles static files automatically (no web.config needed)

---

### Issue 4: Custom domain not working

**Check:**
1. DNS records are correct (Part 7, Step 3)
2. Wait 10-30 minutes for DNS propagation
3. Test with: `nslookup gccwebapp.com`
4. TXT record for domain verification is added

---

## Rollback Plan

**If something goes catastrophically wrong:**

### Your AccuWebHosting database is still safe until you:
1. Verify Azure works completely
2. Run stable for 24-48 hours
3. Make a final backup
4. Cancel the service

### To rollback:
1. Point DNS back to AccuWebHosting
2. Keep using old system
3. Debug Azure issues offline
4. Try migration again when ready

**This is why we DON'T cancel AccuWebHosting immediately!**

---

## Quick Command Reference

```bash
# Redeploy app after code changes
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az webapp deploy --name gccwebapp-prod --resource-group GCCWebApp-RG --src-path ./publish.zip --type zip

# View logs
az webapp log tail --name gccwebapp-prod --resource-group GCCWebApp-RG

# Restart app
az webapp restart --name gccwebapp-prod --resource-group GCCWebApp-RG

# Get connection info
az sql server show --name gccwebapp-sql-server --resource-group GCCWebApp-RG --query fullyQualifiedDomainName --output tsv

# List all resources
az resource list --resource-group GCCWebApp-RG --output table
```

---

## Success Checklist

- [ ] **Part 1:** Azure resources created (15 min)
- [ ] **Part 2:** Database migrated via BACPAC (15 min)
- [ ] **Part 3:** Database verified (data matches) (5 min)
- [ ] **Part 4:** App Service configured (5 min)
- [ ] **Part 5:** Application deployed (5 min)
- [ ] **Part 6:** Deployment verified (app loads, DB works) (5 min)
- [ ] **Part 7:** Custom domain configured (DNS + SSL) (5 min)
- [ ] **Part 8:** Post-migration tasks (passwords, monitoring) (5 min)
- [ ] **Part 9:** Verified stable for 24-48 hours
- [ ] **Part 9:** Canceled AccuWebHosting

**Total Time:** 45 minutes (+ 24-48 hour stability verification before canceling AccuWebHosting)

---

## What You Just Accomplished

**Before (AccuWebHosting):**
- ❌ Weeks of Plesk wrestling
- ❌ HTTP 500 errors with no logs
- ❌ Support tickets for basic tasks
- ❌ Can't deploy your own app
- ❌ No clear error messages

**After (Azure):**
- ✅ Deployed in 45 minutes
- ✅ Database fully migrated
- ✅ Clear logs and monitoring
- ✅ One-command deploys
- ✅ Free SSL certificates
- ✅ Full control over everything
- ✅ Can scale up/down anytime
- ✅ Everything in one place

---

**Generated:** October 10, 2025
**Migration Type:** Complete (App + Database)
**Time:** 45 minutes
**Cost:** ~$18/month
**Status:** Ready to Execute

**NEXT STEP:** Open PowerShell and start Part 1!

# Start Here - Azure Migration for Option 2

**You chose:** Complete migration (App + Database) to Azure
**Goal:** Move everything away from AccuWebHosting
**Time:** 45 minutes total
**Cost:** ~$18/month

---

## What You Need

- [ ] Azure account (sign up at https://portal.azure.com - free $200 credit for 30 days)
- [ ] Azure CLI installed OR use Cloud Shell at https://shell.azure.com
- [ ] SQL Server Management Studio OR Azure Data Studio (for database export/import)
- [ ] Your AccuWebHosting database credentials (you already have these)

---

## Step 1: Install Azure CLI (5 minutes)

### Option A: Install on Windows

```powershell
# Using winget
winget install -e --id Microsoft.AzureCLI

# OR download installer from
# https://aka.ms/installazurecliwindows
```

### Option B: Use Azure Cloud Shell (No Install)

Go to https://shell.azure.com - runs in your browser, no install needed!

---

## Step 2: Run the Automated Deployment Script (10 minutes)

**This script creates all Azure resources for you:**

```powershell
# Open PowerShell in your project directory
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Run the deployment script
.\deploy-azure-complete.ps1
```

**What this does:**
1. Logs you into Azure (browser popup)
2. Creates Resource Group
3. Creates App Service (B1 tier - $13/month)
4. Creates SQL Server
5. Creates SQL Database (Basic tier - $5/month)
6. Configures firewall
7. Builds your app
8. Deploys to Azure
9. Configures all settings

**You'll be prompted for:**
- SQL Admin Password (make it complex!)

**Result:** Your app is live on Azure at https://gccwebapp-prod.azurewebsites.net

---

## Step 3: Migrate Database from AccuWebHosting (15 minutes)

### Using SQL Server Management Studio (SSMS) - RECOMMENDED

#### 3a. Export from AccuWebHosting

1. **Open SSMS**
2. **Connect to AccuWebHosting:**
   - Server: `68.71.129.120,1533`
   - Username: `adminGCC`
   - Password: `Q3Ez4elgE_47KNi`
   - Database: `gccwebap_gccdb`
3. **Export Database:**
   - Right-click database → Tasks → Export Data-tier Application
   - Save to: `C:\Temp\gccwebapp.bacpac`
   - Wait 5-10 minutes

#### 3b. Import to Azure SQL

1. **Connect to Azure SQL:**
   - Server: `gccwebapp-sql-server.database.windows.net`
   - Username: `gccadmin`
   - Password: [what you entered in Step 2]
2. **Import Database:**
   - Right-click Databases → Import Data-tier Application
   - Select file: `C:\Temp\gccwebapp.bacpac`
   - Database: `gccwebapp-db`
   - Wait 5-10 minutes

#### 3c. Verify Migration

Run this SQL script on Azure database:

```sql
-- Run azure-migration-verification.sql
-- Compare results with AccuWebHosting database
```

---

## Step 4: Test Your Application (5 minutes)

**Open these URLs and verify everything works:**

1. **Health Check:**
   ```
   https://gccwebapp-prod.azurewebsites.net/health
   ```
   Expected: "Healthy"

2. **Certificate Tile View:**
   ```
   https://gccwebapp-prod.azurewebsites.net/Certificate/CertificateTileView
   ```
   Expected: Your certificates load (proves database works!)

3. **Login:**
   ```
   https://gccwebapp-prod.azurewebsites.net/Account/Login
   ```
   Credentials:
   - Email: `admin@greencirclecertified.com`
   - Password: `Admin123!`

   Expected: Login works, dashboard loads

4. **Test Key Features:**
   - View certificate details
   - Search/filter certificates
   - Access admin panel
   - Upload files
   - Export data

**If everything works → Move to Step 5**
**If issues → Check logs:** `az webapp log tail --name gccwebapp-prod --resource-group GCCWebApp-RG`

---

## Step 5: Configure Custom Domain (10 minutes)

### 5a. Add Domain in Azure

```bash
az webapp config hostname add --webapp-name gccwebapp-prod --resource-group GCCWebApp-RG --hostname gccwebapp.com
az webapp config hostname add --webapp-name gccwebapp-prod --resource-group GCCWebApp-RG --hostname www.gccwebapp.com
```

### 5b. Get DNS Information

```bash
# Get IP address for A record
az webapp show --name gccwebapp-prod --resource-group GCCWebApp-RG --query inboundIpAddress --output tsv

# Get hostname for CNAME
az webapp show --name gccwebapp-prod --resource-group GCCWebApp-RG --query defaultHostName --output tsv
```

### 5c. Update DNS at Your Registrar

**Add these records where you registered gccwebapp.com:**

1. **A Record:**
   - Type: A
   - Name: @ (or blank)
   - Value: [IP from command above]
   - TTL: 3600

2. **CNAME Record:**
   - Type: CNAME
   - Name: www
   - Value: gccwebapp-prod.azurewebsites.net
   - TTL: 3600

**Wait 10-30 minutes for DNS to propagate**

### 5d. Enable FREE SSL

```bash
az webapp config ssl create --name gccwebapp-prod --resource-group GCCWebApp-RG --hostname gccwebapp.com
az webapp config ssl create --name gccwebapp-prod --resource-group GCCWebApp-RG --hostname www.gccwebapp.com
az webapp config ssl bind --name gccwebapp-prod --resource-group GCCWebApp-RG --certificate-thumbprint auto --ssl-type SNI
```

**Test:** https://gccwebapp.com (should work with SSL!)

---

## Step 6: Post-Migration Checklist

### Security Tasks (CRITICAL)

- [ ] Change all default passwords:
  - Admin: admin@greencirclecertified.com
  - Analyst: analyst@greencirclecertified.com
  - External: partner@amazon.com

### Verification Tasks

- [ ] All certificates visible and searchable
- [ ] Login/logout works
- [ ] File uploads work
- [ ] Admin panel accessible
- [ ] API endpoints work (if you use them)
- [ ] Custom domain works with HTTPS
- [ ] No errors in logs

### Monitoring Setup (Optional but Recommended)

```bash
# Create Application Insights (free tier)
az monitor app-insights component create --app gccwebapp-insights --location eastus --resource-group GCCWebApp-RG --application-type web

# Get instrumentation key
$insightsKey = az monitor app-insights component show --app gccwebapp-insights --resource-group GCCWebApp-RG --query instrumentationKey --output tsv

# Link to app
az webapp config appsettings set --name gccwebapp-prod --resource-group GCCWebApp-RG --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$insightsKey"
```

---

## Step 7: Run Stable Before Canceling AccuWebHosting

**DO NOT cancel AccuWebHosting immediately!**

### Stability Testing (24-48 hours)

- [ ] Day 1: Monitor for errors, test all features
- [ ] Day 2: Verify no issues, check logs
- [ ] Day 3: Make final backup of AccuWebHosting database
- [ ] Day 4: Cancel AccuWebHosting services

### What to Monitor

```bash
# Watch logs in real-time
az webapp log tail --name gccwebapp-prod --resource-group GCCWebApp-RG

# Check for errors
# Look for: Exceptions, database connection issues, failed requests
```

### Rollback Plan (Just in Case)

If something goes catastrophically wrong:

1. AccuWebHosting database is still safe (you didn't cancel yet!)
2. Point DNS back to AccuWebHosting
3. Debug Azure issues offline
4. Try migration again

---

## Quick Commands Reference

```bash
# View real-time logs
az webapp log tail --name gccwebapp-prod --resource-group GCCWebApp-RG

# Restart app
az webapp restart --name gccwebapp-prod --resource-group GCCWebApp-RG

# Redeploy after code changes
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az webapp deploy --name gccwebapp-prod --resource-group GCCWebApp-RG --src-path ./publish.zip --type zip

# Stop app (for maintenance)
az webapp stop --name gccwebapp-prod --resource-group GCCWebApp-RG

# Start app
az webapp start --name gccwebapp-prod --resource-group GCCWebApp-RG

# List all resources
az resource list --resource-group GCCWebApp-RG --output table

# Delete everything (if you need to start over)
az group delete --name GCCWebApp-RG --yes
```

---

## Cost Breakdown

**Monthly Costs:**
- App Service (B1): $13/month
- Azure SQL (Basic): $5/month
- SSL Certificate: FREE (Let's Encrypt)
- **Total: ~$18/month**

**vs AccuWebHosting:**
- AccuWebHosting: $?/month + weeks of headaches
- Azure: $18/month + deployed in 45 minutes

**Upgrade Options (if needed later):**
- App Service S1: $75/month (auto-scaling, staging slots, backups)
- Azure SQL Standard: $15/month (10 GB, better performance)

---

## Troubleshooting

### "Cannot connect to database"

**Check:**
1. Firewall allows Azure services (should be automatic)
2. Connection string is correct
3. Database has data (run verification script)

**Fix:**
```bash
az webapp log tail --name gccwebapp-prod --resource-group GCCWebApp-RG
# Look for SqlException errors
```

### "Application Error 500.30"

**Check:**
1. Environment variable is set to Production
2. Runtime is DOTNET 8.0

**Fix:**
```bash
az webapp config show --name gccwebapp-prod --resource-group GCCWebApp-RG
```

### Custom domain not working

**Check:**
1. DNS records are correct
2. Wait 10-30 minutes for DNS propagation
3. Test: `nslookup gccwebapp.com`

---

## Support Resources

**Azure Documentation:**
- App Service: https://docs.microsoft.com/azure/app-service/
- Azure SQL: https://docs.microsoft.com/azure/sql-database/

**Your Migration Guides:**
- Complete guide: `AZURE_COMPLETE_MIGRATION.md`
- Database verification: `azure-migration-verification.sql`
- Deployment script: `deploy-azure-complete.ps1`

**Azure Support:**
- Free tier: Community support (Stack Overflow)
- Pay-as-you-go: $29/month for Developer support

---

## Success Criteria

**You're done when:**
- [ ] App loads at https://gccwebapp.com
- [ ] SSL certificate is active (green padlock)
- [ ] All certificates visible and searchable
- [ ] Login/admin features work
- [ ] Database fully migrated and verified
- [ ] No errors in logs
- [ ] Stable for 24-48 hours
- [ ] AccuWebHosting canceled (after 48 hours stable)

---

## What You Accomplished

**Before (AccuWebHosting/Plesk):**
- ❌ Weeks of troubleshooting
- ❌ HTTP 500 errors
- ❌ Support tickets for basic tasks
- ❌ No control over environment

**After (Azure):**
- ✅ Deployed in 45 minutes
- ✅ Complete control
- ✅ Clear error logs
- ✅ One-command deployments
- ✅ Free SSL
- ✅ Scalable anytime
- ✅ Everything in one place

---

**Ready to start?**

**Next Action:** Open PowerShell and run Step 2!

```powershell
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
.\deploy-azure-complete.ps1
```

**Good luck! You've got this!** 🚀

# Azure PostgreSQL Database Setup - Quick Guide

## Step 1: Create Azure Database for PostgreSQL (5-10 minutes)

1. **Go to Azure Portal**: https://portal.azure.com
2. **Click "Create a resource"** (top left, or the + icon)
3. **Search for "Azure Database for PostgreSQL"** and select it
4. **Click "Create"** button
5. **Choose "Flexible server"** (recommended for cost-effectiveness)

### Configuration Settings:

**Basics Tab:**
- **Subscription**: Select your subscription
- **Resource Group**: Create new or use existing (e.g., "gcc-webapp-rg")
- **Server name**: `gccwebapp-db` (must be globally unique, try adding numbers if taken)
- **Region**: Choose closest to you (e.g., "East US" or "West US 2")
- **PostgreSQL version**: 16 (latest stable)
- **Workload type**: Select "Development" (lowest cost ~$12-15/month)
- **Compute + storage**: Click "Configure server"
  - Select **Burstable** tier
  - Select **B1ms** (1 vCore, 2 GiB RAM) - cheapest option
  - Storage: 32 GiB (minimum)
  - Click "Save"

**Authentication Tab:**
- **Admin username**: `gccadmin` (write this down!)
- **Password**: Create a strong password (write this down!)
- **Confirm password**: Re-enter password

**Networking Tab:**
- **Connectivity method**: Select "Public access (allowed IP addresses)"
- Check "Allow public access from any Azure service within Azure to this server"
- Click "+ Add 0.0.0.0 - 255.255.255.255" to allow all IPs (for development - restrict later)

**Review + Create Tab:**
- Review pricing (should be ~$12-15/month)
- Click **"Create"**
- Wait 5-10 minutes for deployment

---

## Step 2: Get Your Connection String (2 minutes)

1. **After deployment completes**, click "Go to resource"
2. **In left menu**, click "Connection strings" under Settings
3. **Copy the ADO.NET connection string** - it will look like:
   ```
   Host=gccwebapp-db.postgres.database.azure.com;Database=postgres;Username=gccadmin;Password={your_password};SSL Mode=Require;Trust Server Certificate=true
   ```
4. **Replace `{your_password}`** with your actual password
5. **Replace `Database=postgres`** with `Database=gccdb` (or your preferred database name)

**Final connection string example:**
```
Host=gccwebapp-db.postgres.database.azure.com;Database=gccdb;Username=gccadmin;Password=YourStrongPassword123!;SSL Mode=Require;Trust Server Certificate=true
```

---

## Step 3: Test Connection Locally (5 minutes)

1. **Open** `appsettings.Development.json`
2. **Replace** the ConnectionStrings section:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=gccwebapp-db.postgres.database.azure.com;Database=gccdb;Username=gccadmin;Password=YourStrongPassword123!;SSL Mode=Require;Trust Server Certificate=true"
   }
   ```
3. **Run migrations**:
   ```bash
   dotnet ef database update
   ```
4. **Test the app**:
   ```bash
   dotnet run
   ```
5. **Verify** it connects without errors

---

## Step 4: Create Azure App Service (5-10 minutes)

1. **Go to Azure Portal**: https://portal.azure.com
2. **Click "Create a resource"**
3. **Search for "Web App"** and select it
4. **Click "Create"**

### Configuration Settings:

**Basics Tab:**
- **Subscription**: Select your subscription
- **Resource Group**: Use same as database (e.g., "gcc-webapp-rg")
- **Name**: `gccwebapp` (must be globally unique, URL will be gccwebapp.azurewebsites.net)
- **Publish**: Code
- **Runtime stack**: .NET 8 (LTS)
- **Operating System**: Linux (cheaper than Windows)
- **Region**: Same as your database
- **Pricing Plan**: Create new
  - Select **Basic B1** (~$13/month)
  - Click "OK"

**Deployment Tab:**
- Enable GitHub Actions: Optional (can set up later)

**Review + Create Tab:**
- Review pricing (should be ~$13/month)
- Click **"Create"**
- Wait 3-5 minutes for deployment

---

## Step 5: Configure App Service (3 minutes)

1. **After deployment**, click "Go to resource"
2. **In left menu**, click "Configuration" under Settings
3. **Click "+ New connection string"**:
   - **Name**: `DefaultConnection`
   - **Value**: Paste your PostgreSQL connection string
   - **Type**: PostgreSQL
   - Click "OK"
4. **Click "Save"** at the top
5. **Click "Continue"** when prompted

---

## Step 6: Deploy Your App (5-10 minutes)

### Option A: Deploy from Visual Studio (Easiest)
1. Right-click project in Solution Explorer
2. Click "Publish"
3. Select "Azure"
4. Select "Azure App Service (Linux)"
5. Sign in and select your app service
6. Click "Finish" and "Publish"

### Option B: Deploy from Command Line
```bash
# Install Azure CLI if needed: https://aka.ms/installazurecli

# Login
az login

# Publish app locally
dotnet publish -c Release -o ./publish

# Create deployment zip
cd publish
tar -czf ../deploy.tar.gz *
cd ..

# Deploy to Azure
az webapp deployment source config-zip --resource-group gcc-webapp-rg --name gccwebapp --src deploy.tar.gz
```

---

## Step 7: Verify Deployment (2 minutes)

1. **Go to your App Service** in Azure Portal
2. **Click "Browse"** (or visit https://gccwebapp.azurewebsites.net)
3. **Your app should load!**
4. Test login and certificate views

---

## Total Estimated Cost

- **Azure Database for PostgreSQL** (Burstable B1ms): ~$12-15/month
- **App Service** (Basic B1): ~$13/month
- **Total**: ~$25-28/month ✅

---

## Troubleshooting

### Error: "Could not connect to server"
- Check firewall rules in PostgreSQL "Networking" settings
- Ensure "Allow public access" is enabled
- Verify connection string password is correct

### Error: "SSL connection required"
- Ensure connection string includes `SSL Mode=Require`

### Error: "Database does not exist"
- Run `dotnet ef database update` to create database schema

### App shows errors after deployment
- Check App Service logs: Go to "Log stream" in left menu
- Verify connection string in Configuration
- Check that .NET 8 runtime is selected

---

## Security Notes (Do After Demo)

1. **Restrict Database Firewall**: Remove 0.0.0.0-255.255.255.255 rule, add only App Service IPs
2. **Use Key Vault**: Store connection strings in Azure Key Vault
3. **Enable SSL**: Already enabled, but verify certificate validation
4. **Review Authentication**: Ensure production authentication is configured

---

## Need Help?

If you encounter issues during your status call, share:
1. The specific error message
2. Which step you're on
3. Screenshot if helpful

Good luck with your status call!
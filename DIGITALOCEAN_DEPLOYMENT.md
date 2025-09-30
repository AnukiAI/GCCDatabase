# DigitalOcean Deployment Guide - GCC Web App

## Total Time: 25-30 minutes
## Total Cost: $5-12/month

---

## Step 1: Create DigitalOcean Account (3 minutes)

1. Go to: https://www.digitalocean.com/
2. Click "Sign Up"
3. Use GitHub to sign up (easiest) or email
4. **New users get $200 credit for 60 days!** 🎉
5. Add payment method (required but won't charge during trial)

---

## Step 2: Push Your Code to GitHub (5 minutes)

**If not already on GitHub:**

```bash
# Navigate to your project
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Initialize git (if needed)
git init

# Add all files
git add .

# Commit
git commit -m "Prepare for DigitalOcean deployment"

# Create repo on GitHub (go to github.com, create new repo named "GCCWebApp")

# Add remote and push
git remote add origin https://github.com/YOUR_USERNAME/GCCWebApp.git
git branch -M main
git push -u origin main
```

**If already on GitHub:**
```bash
# Just make sure latest changes are pushed
git add .
git commit -m "Ready for deployment"
git push
```

---

## Step 3: Create Managed PostgreSQL Database (5 minutes)

1. **Log into DigitalOcean**: https://cloud.digitalocean.com/
2. **Click "Create" → "Databases"**
3. **Choose PostgreSQL** (version 16)
4. **Select Plan**:
   - **Development** (recommended for dev): $7/month
     - 1 GB RAM, 1 vCPU, 10 GB disk
   - OR **Basic**: $15/month (if you need more performance)
     - 1 GB RAM, 1 vCPU, 25 GB disk

5. **Choose Region**: Select closest to you (e.g., "New York", "San Francisco")
6. **Database Name**: `gccdb`
7. **Name your cluster**: `gccwebapp-db`
8. **Click "Create Database Cluster"**
9. **Wait 3-5 minutes** for provisioning

---

## Step 4: Get Database Connection String (2 minutes)

1. **After database is ready**, you'll see the overview page
2. **Click "Connection Details"**
3. **Connection Parameters** section shows:
   - Host
   - Port
   - Username
   - Password
   - Database

4. **Copy the connection string** - format for .NET:
   ```
   Host=your-db-name.db.ondigitalocean.com;Port=25060;Database=gccdb;Username=doadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
   ```

5. **Save this somewhere safe!**

**Example:**
```
Host=gccwebapp-db-do-user-12345-0.c.db.ondigitalocean.com;Port=25060;Database=gccdb;Username=doadmin;Password=abc123xyz789;SSL Mode=Require;Trust Server Certificate=true
```

---

## Step 5: Add Trusted Sources (Important for Security)

1. In database overview, scroll to **"Trusted Sources"**
2. Click **"Edit"**
3. For now, add: **"All sources"** (you'll restrict this after deployment)
4. Click **"Save"**

---

## Step 6: Test Database Connection Locally (5 minutes)

Before deploying, verify the connection works:

1. **Open** `appsettings.Development.json`
2. **Update connection string**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=gccwebapp-db-do-user-12345-0.c.db.ondigitalocean.com;Port=25060;Database=gccdb;Username=doadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
     }
   }
   ```

3. **Run migrations**:
   ```bash
   dotnet ef database update
   ```

4. **You should see**: "Applying migration..." and "Done."

5. **Test the app**:
   ```bash
   dotnet run
   ```

6. **Open browser**: http://localhost:5101
7. **Verify app loads and you can login**

**If it works locally, it will work on DigitalOcean!** ✅

---

## Step 7: Create App Platform Web Service (5 minutes)

1. **Go to DigitalOcean Dashboard**
2. **Click "Create" → "Apps"**
3. **Choose "GitHub"** as source
4. **Authorize DigitalOcean** to access your GitHub
5. **Select your repository**: `GCCWebApp`
6. **Select branch**: `main` (or `master`)
7. **Click "Next"**

---

## Step 8: Configure App Settings (5 minutes)

### Resources Configuration:

1. **DigitalOcean will auto-detect** your app
2. **Source Directory**: Leave as `/` (or set to `/GCCWebApp` if in subfolder)
3. **Build Command**:
   ```
   dotnet publish -c Release -o out
   ```
4. **Run Command**:
   ```
   dotnet out/GCCWebApp.dll
   ```

### Environment Variables:

1. **Click "Edit" next to your app component**
2. **Scroll to "Environment Variables"**
3. **Click "Add Variable"** and add each:

   ```
   Key: ASPNETCORE_ENVIRONMENT
   Value: Production
   ```

   ```
   Key: ASPNETCORE_URLS
   Value: http://+:8080
   ```

   ```
   Key: ConnectionStrings__DefaultConnection
   Value: [Paste your PostgreSQL connection string from Step 4]
   ```

   **Note**: DigitalOcean uses double underscore `__` for nested config!

### Plan Selection:

1. **Scroll to "Plan"**
2. **Select "Basic"**: $5/month
   - 512 MB RAM, 1 vCPU
   - Perfect for dev/demo

3. **Region**: Same as your database

4. **Click "Next"**

---

## Step 9: Add Database to App (2 minutes)

1. **On "Add Resources" screen**
2. **Click "Add Resource"**
3. **Select "Database"**
4. **Choose your database**: `gccwebapp-db`
5. **Click "Attach"**
6. DigitalOcean will auto-inject `DATABASE_URL` environment variable

**Note**: Your `ConnectionStrings__DefaultConnection` variable will take precedence, which is what we want.

---

## Step 10: Review and Deploy (2 minutes)

1. **Review all settings**:
   - Source: GitHub repo
   - Branch: main
   - Build/Run commands configured
   - Environment variables set
   - Database attached
   - Plan: $5/month

2. **Name your app**: `gccwebapp` (or whatever you prefer)
   - URL will be: `gccwebapp-xxxxx.ondigitalocean.app`

3. **Click "Create Resources"**

4. **Wait 5-10 minutes** for build and deployment

---

## Step 11: Monitor Deployment (5 minutes)

1. **You'll see the build logs** in real-time
2. **Look for**:
   - "Building..."
   - "Running dotnet publish..."
   - "Build successful"
   - "Deploying..."
   - "Live"

3. **If build fails**, check:
   - Build command syntax
   - Run command points to correct DLL
   - All packages restore correctly

---

## Step 12: Access Your App (1 minute)

1. **Once status shows "Live"**
2. **Click the URL** (or copy it): `https://gccwebapp-xxxxx.ondigitalocean.app`
3. **Your app should load!** 🎉
4. **Test**:
   - Certificate tile view
   - Login with test credentials
   - Create/edit certificates
   - Search functionality

---

## Step 13: Custom Domain (Optional, 5 minutes later)

If you have a domain:

1. **Go to App Settings → "Domains"**
2. **Click "Add Domain"**
3. **Enter your domain**: `www.greencirclecertified.com`
4. **DigitalOcean provides DNS instructions**
5. **Add CNAME record** in your domain registrar
6. **Free SSL certificate** auto-provisioned

---

## Troubleshooting

### Build Fails: "Could not find project or directory"
- Check "Source Directory" setting
- If your .csproj is in a subfolder, set it to `/GCCWebApp`

### Build Fails: "Package restore failed"
- Ensure `GCCWebApp.csproj` is in the repository
- Check that all PackageReferences are valid

### App Runs But Shows Errors
- Check "Runtime Logs" in DigitalOcean console
- Verify database connection string is correct
- Ensure migrations ran (see Step 6)

### Database Connection Fails
- Verify "Trusted Sources" includes your app
- Check connection string format (use double underscore in env vars)
- Confirm database is "Available" status

### App Shows "503 Service Unavailable"
- Check that port 8080 is configured in `ASPNETCORE_URLS`
- Verify Run Command points to correct DLL path
- Check Runtime Logs for startup errors

---

## Post-Deployment Security

### 1. Restrict Database Access
- Go to database → "Trusted Sources"
- Remove "All sources"
- Add only: Your App Platform resource
- Click "Save"

### 2. Add Production Secrets
- Move JWT secret to environment variable
- Add to App Platform settings:
  ```
  Jwt__Key: [Generate new 64+ char random string]
  ```

### 3. Enable Azure AD (if using)
- Add Azure AD settings as environment variables
- Test authentication flow

---

## Scaling & Monitoring

### View Logs:
- App Platform → Your App → "Runtime Logs"
- Real-time log streaming
- Filter by severity

### View Metrics:
- CPU usage
- Memory usage
- Request rates
- Response times

### Scale Up:
- Increase plan to Professional ($12/month) for 1 GB RAM
- Auto-scaling available at higher tiers

### Database Backups:
- Automatic daily backups included
- Point-in-time recovery available
- Manual backup/restore through dashboard

---

## Monthly Costs Summary

| Resource | Plan | Cost |
|----------|------|------|
| App Platform | Basic (512 MB) | $5 |
| PostgreSQL | Development | $7 |
| **Total** | | **$12/month** |

**With $200 credit**: Free for 16+ months! 🎉

---

## Quick Commands Reference

```bash
# Push updates to deploy automatically
git add .
git commit -m "Update feature"
git push origin main

# View app logs (install doctl CLI)
doctl apps logs YOUR_APP_ID --follow

# Check database
psql "$DATABASE_URL"

# Run migrations locally against production DB
# (use production connection string in appsettings)
dotnet ef database update
```

---

## Support Resources

- **DigitalOcean Docs**: https://docs.digitalocean.com/products/app-platform/
- **.NET on App Platform**: https://docs.digitalocean.com/products/app-platform/languages-frameworks/dotnet/
- **Community**: https://www.digitalocean.com/community/

---

## For Your Status Call

**Show them**:
1. Live URL: `https://gccwebapp-xxxxx.ondigitalocean.app`
2. DigitalOcean dashboard (professional appearance)
3. Cost: $12/month (vs Azure $25-28/month)
4. Deployed from GitHub (CI/CD setup)
5. Managed database with automatic backups
6. Can scale as needed

**Key talking points**:
- ✅ Production-ready hosting
- ✅ 50% cost savings vs Azure
- ✅ Easy to maintain and update
- ✅ Automatic deployments from GitHub
- ✅ Professional managed database
- ✅ Free for 16+ months with $200 credit

Good luck! 🚀
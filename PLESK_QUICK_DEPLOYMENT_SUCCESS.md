# GCC Web App - Plesk Deployment Guide (WORKING VERSION)
## Successfully Deployed to angry-pascal.70-32-82-212.plesk.page

This guide documents the ACTUAL WORKING steps for deploying the GCC Web Application to a Plesk server without SSH access.

---

## Prerequisites
- Plesk Panel access
- Files from your local project:
  - `docker-deploy-plesk-registry-MARIADB.cmd`
  - `.env.plesk-UPDATED`
  - `plesk-nginx-config.conf`

---

## Step 1: Upload Files via Plesk File Manager

1. **Login to Plesk Panel**
2. **Click "Files"** in the left sidebar
3. **Navigate to `/httpdocs` folder**
4. **Create new folder:** Click "New" → "Create Directory" → Name it `deployment`
5. **Enter the `deployment` folder**
6. **Upload files:** Click "Upload" and select:
   - `docker-deploy-plesk-registry-MARIADB.cmd`
   - `.env.plesk-UPDATED`
   - `plesk-nginx-config.conf`
7. **Rename file:** Select `.env.plesk-UPDATED` → Click "Rename" → Change to `.env`

---

## Step 2: Create Linux Deployment Script

1. **In File Manager** (still in `/httpdocs/deployment`):
2. **Click "New" → "Create File"** → Name it `deploy-linux.sh`
3. **Click on `deploy-linux.sh` to edit**
4. **Paste this exact content:**

```bash
#!/bin/bash
echo "======================================"
echo "GCC Web App Docker Deployment"
echo "======================================"

# Login to GitHub Container Registry
echo "Logging into GitHub Container Registry..."
echo "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" | docker login ghcr.io -u AnukiAI --password-stdin

# Pull the latest image
echo "Pulling latest image..."
docker pull ghcr.io/anukiai/gcc-webapp:latest

# Stop and remove existing container if it exists
echo "Stopping existing container..."
docker stop gcc-webapp 2>/dev/null
docker rm gcc-webapp 2>/dev/null

# Run the new container
echo "Starting new container..."
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://0.0.0.0:8080" \
  -e ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" \
  -e Jwt__Key="a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" \
  -e Jwt__Issuer="https://angry-pascal.70-32-82-212.plesk.page" \
  -e Jwt__Audience="https://angry-pascal.70-32-82-212.plesk.page" \
  ghcr.io/anukiai/gcc-webapp:latest

# Check if container started
sleep 5
if docker ps | grep gcc-webapp; then
    echo "✓ Container started successfully!"
    echo "Application running on port 5102"
else
    echo "✗ Container failed to start. Check logs:"
    docker logs gcc-webapp
fi
```

5. **Save the file**

---

## Step 3: Execute Deployment via Scheduled Tasks

1. **Go to "Tools & Settings"** in Plesk sidebar
2. **Click "Scheduled Tasks (Cron Jobs)"**
3. **Click "Add Task"**
4. **Configure task:**
   - **Command:** `chmod +x /var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/deployment/deploy-linux.sh`
   - **Run:** Run command
   - **User:** root
5. **Save and run once** (makes script executable)

6. **Edit the task again:**
   - **Change command to:** `/bin/bash /var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/deployment/deploy-linux.sh`
7. **Save and run** (this deploys the container)

---

## Step 4: Verify Deployment

**Expected successful output:**
```
======================================
GCC Web App Docker Deployment
======================================
Logging into GitHub Container Registry...
Login Succeeded
Pulling latest image...
latest: Pulling from anukiai/gcc-webapp
Status: Image is up to date for ghcr.io/anukiai/gcc-webapp:latest
Stopping existing container...
gcc-webapp
Starting new container...
5aa65cd7c7b...
✓ Container started successfully!
Application running on port 5102
```

**Test container health:**
- Create another scheduled task with command: `curl http://localhost:5102/health`
- Should return health check response

---

## Step 5: Configure Nginx (Next Step)

The container is now running on port 5102, but needs nginx configuration to be accessible via the domain.

1. Navigate to domain settings in Plesk
2. Find "Apache & nginx Settings"
3. Add proxy configuration to forward traffic to port 5102

---

## Common Issues & Solutions

### Issue: "No such file or directory"
**Solution:** Use full path: `/var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/deployment/deploy-linux.sh`

### Issue: "Permission denied"
**Solution:** Run `chmod +x` command first to make script executable

### Issue: Command has typos or spaces
**Solution:**
- Ensure space between `/bin/bash` and path
- Check for typos (deploy-linux.sh not deploy-linus.sh)
- No extra characters at end of command

### Issue: Docker not found
**Solution:** Docker must be installed on the Plesk server. Contact hosting provider if missing.

---

## Files Location Summary

All deployment files are located in:
```
/var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/deployment/
```

- `deploy-linux.sh` - Main deployment script
- `.env` - Environment configuration
- `plesk-nginx-config.conf` - Nginx configuration (to be applied)
- `docker-deploy-plesk-registry-MARIADB.cmd` - Windows reference script

---

## Quick Re-deployment

To redeploy or update the container:
1. Go to Scheduled Tasks
2. Find your deployment task
3. Click "Run Now"

The script will:
- Pull latest image
- Stop old container
- Start new container
- Preserve all settings

---

## Status: Container Running ✅

- Docker container: **RUNNING** (ID: 5aa65cd7c7b...)
- Port: **5102**
- Database: **MariaDB configured**
- Next step: **Configure nginx proxy**

---

*Last updated: September 15, 2025*
*Deployment completed successfully via Plesk Scheduled Tasks without SSH access*
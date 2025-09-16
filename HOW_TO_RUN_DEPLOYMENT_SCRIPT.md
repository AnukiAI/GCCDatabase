# How to Run the Deployment Script on Your Plesk Server

## Step 1: Upload Files to Your Plesk Server

### Files You Need to Upload:
1. `docker-deploy-plesk-registry-MARIADB.cmd` - The deployment script
2. `.env.plesk-UPDATED` - Environment configuration (optional)
3. `plesk-nginx-config.conf` - Nginx configuration

### Method A: Using Plesk File Manager (Easiest)

1. **Login to Plesk Panel**
   - Go to: https://angry-pascal.70-32-82-212.plesk.page:8443
   - Login with your Plesk admin credentials

2. **Access File Manager**
   - Click on **Files** in the left menu
   - Navigate to your home directory or create a new folder:
   ```
   /opt/gcc-webapp/
   ```

3. **Create Directory** (if not exists)
   - Click **Create** → **Directory**
   - Name it: `gcc-webapp`
   - Navigate into this directory

4. **Upload Files**
   - Click **Upload** button
   - Select `docker-deploy-plesk-registry-MARIADB.cmd`
   - Click **Open** and wait for upload
   - Repeat for other files if needed

### Method B: Using SSH/SCP

1. **Connect to Your Server via SSH**
   ```bash
   ssh root@angry-pascal.70-32-82-212.plesk.page
   # OR
   ssh your-username@angry-pascal.70-32-82-212.plesk.page
   ```

2. **Create Directory**
   ```bash
   sudo mkdir -p /opt/gcc-webapp
   cd /opt/gcc-webapp
   ```

3. **Upload Files from Your Local Machine**
   Open a NEW terminal on your local machine:
   ```bash
   # Upload the deployment script
   scp "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\docker-deploy-plesk-registry-MARIADB.cmd" root@angry-pascal.70-32-82-212.plesk.page:/opt/gcc-webapp/

   # Upload the environment file
   scp "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\.env.plesk-UPDATED" root@angry-pascal.70-32-82-212.plesk.page:/opt/gcc-webapp/.env

   # Upload nginx config
   scp "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\plesk-nginx-config.conf" root@angry-pascal.70-32-82-212.plesk.page:/opt/gcc-webapp/
   ```

### Method C: Copy and Paste Content

1. **SSH to your server**
   ```bash
   ssh root@angry-pascal.70-32-82-212.plesk.page
   cd /opt/gcc-webapp
   ```

2. **Create the script file**
   ```bash
   nano deploy.sh
   ```

3. **Copy the content** from `docker-deploy-plesk-registry-MARIADB.cmd` and paste it into nano
4. **Save and exit**: Press `Ctrl+X`, then `Y`, then `Enter`

---

## Step 2: Convert Windows Script to Linux (Your Plesk runs Linux)

Since Plesk servers typically run Linux, you need to convert the Windows `.cmd` script:

### Option A: Quick Conversion
```bash
cd /opt/gcc-webapp

# Convert Windows line endings to Linux
dos2unix docker-deploy-plesk-registry-MARIADB.cmd

# OR if dos2unix is not installed:
sed -i 's/\r$//' docker-deploy-plesk-registry-MARIADB.cmd

# Rename to .sh
mv docker-deploy-plesk-registry-MARIADB.cmd deploy.sh

# Make it executable
chmod +x deploy.sh
```

### Option B: Create Linux Version Directly
```bash
cd /opt/gcc-webapp

# Create the deployment script
cat > deploy.sh << 'EOF'
#!/bin/bash
# ============================================================
# GCC Web App - Docker Deployment Script for Plesk Server
# MariaDB Version - Linux
# ============================================================

echo "===================================="
echo "GCC Web App Docker Deployment (MariaDB)"
echo "===================================="

# MariaDB Configuration
MYSQL_HOST="localhost"
MYSQL_DATABASE="gccdb"
MYSQL_USER="gccuser"
MYSQL_PASSWORD="74Fzs0YFLMXLrKJG5fzexsTkk"
MYSQL_PORT="3306"

DOMAIN="angry-pascal.70-32-82-212.plesk.page"
JWT_SECRET="a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047"

# Docker configuration
CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:latest"
EXTERNAL_PORT="5102"
INTERNAL_PORT="8080"

echo "Pulling latest image from GitHub Container Registry..."
docker pull $IMAGE_NAME

if [ $? -ne 0 ]; then
    echo "❌ Failed to pull image from registry"
    exit 1
fi

# Stop and remove existing container
echo "Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

# Run container with MariaDB configuration
echo "Starting container with MariaDB configuration..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p ${EXTERNAL_PORT}:${INTERNAL_PORT} \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://0.0.0.0:${INTERNAL_PORT}" \
  -e ASPNETCORE_HTTP_PORTS="${INTERNAL_PORT}" \
  -e ConnectionStrings__DefaultConnection="Server=${MYSQL_HOST};Port=${MYSQL_PORT};Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};" \
  -e Jwt__Key="${JWT_SECRET}" \
  -e Jwt__Issuer="https://${DOMAIN}" \
  -e Jwt__Audience="https://${DOMAIN}" \
  -e GCC_AZURE_AD_B2C_CLIENT_SECRET="dummy-secret" \
  -e GCC_AZURE_AD_CLIENT_SECRET="dummy-secret" \
  -e GCC_APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e GCC_SMTP_PASSWORD="dummy-password" \
  $IMAGE_NAME

# Check if container started
sleep 10
docker ps | grep $CONTAINER_NAME > /dev/null

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Container started successfully!"
    echo ""
    echo "Application is running on port $EXTERNAL_PORT"
    echo "Database: MariaDB $MYSQL_HOST:$MYSQL_PORT/$MYSQL_DATABASE"
    echo "Domain: $DOMAIN"
    echo ""
    echo "Next steps:"
    echo "1. Configure Plesk nginx proxy"
    echo "2. Set up SSL certificate"
    echo "3. Test the application"
    echo ""
    echo "Test locally: curl http://localhost:$EXTERNAL_PORT/health"
    echo "View logs: docker logs -f $CONTAINER_NAME"
    echo ""

    # Test health endpoint
    echo "Testing health endpoint..."
    sleep 5
    curl -s http://localhost:$EXTERNAL_PORT/health
    if [ $? -eq 0 ]; then
        echo "✅ Health endpoint responding"
    else
        echo "⚠️ Health endpoint not responding yet"
    fi
else
    echo "❌ Container failed to start. Check logs:"
    docker logs $CONTAINER_NAME
    exit 1
fi
EOF

# Make executable
chmod +x deploy.sh
```

---

## Step 3: Run the Deployment Script

### Prerequisites Check
```bash
# Check Docker is installed
docker --version

# Check if logged into GitHub Container Registry
docker pull ghcr.io/anukiai/gcc-webapp:latest
```

### Run the Script
```bash
cd /opt/gcc-webapp

# Run the deployment
./deploy.sh

# OR if you get permission denied:
sudo ./deploy.sh

# OR run with bash directly:
bash deploy.sh
```

---

## Step 4: Verify Deployment

### Check Container Status
```bash
# See if container is running
docker ps

# You should see:
CONTAINER ID   IMAGE                              COMMAND         STATUS
xxxxxxxxxxxx   ghcr.io/anukiai/gcc-webapp:latest "dotnet GCC..."  Up 2 minutes
```

### Check Application Logs
```bash
# View logs
docker logs gcc-webapp

# Follow logs in real-time
docker logs -f gcc-webapp
```

### Test the Application
```bash
# Test health endpoint
curl http://localhost:5102/health

# Test main page
curl -I http://localhost:5102
```

---

## Troubleshooting

### If Script Fails: "Command not found"
```bash
# The script has Windows line endings, fix it:
sed -i 's/\r$//' deploy.sh
```

### If Docker Pull Fails: "unauthorized"
```bash
# Login to GitHub Container Registry
echo "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" | docker login ghcr.io -u AnukiAI --password-stdin
```

### If Port 5102 is Already in Use
```bash
# Find what's using the port
netstat -tulpn | grep 5102

# Change the EXTERNAL_PORT in the script to another port (e.g., 5103)
nano deploy.sh
# Change: EXTERNAL_PORT="5103"
```

### If Database Connection Fails
```bash
# Test MariaDB connection
mysql -h localhost -P 3306 -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk gccdb -e "SELECT 1;"

# If fails, create database and user:
mysql -u root -p
```
Then run:
```sql
CREATE DATABASE gccdb CHARACTER SET utf8mb4;
CREATE USER 'gccuser'@'localhost' IDENTIFIED BY '74Fzs0YFLMXLrKJG5fzexsTkk';
CREATE USER 'gccuser'@'127.0.0.1' IDENTIFIED BY '74Fzs0YFLMXLrKJG5fzexsTkk';
GRANT ALL PRIVILEGES ON gccdb.* TO 'gccuser'@'localhost';
GRANT ALL PRIVILEGES ON gccdb.* TO 'gccuser'@'127.0.0.1';
FLUSH PRIVILEGES;
```

---

## Quick One-Liner Deployment

If you want to do everything in one command after SSH:
```bash
cd /opt/gcc-webapp && \
curl -o deploy.sh https://raw.githubusercontent.com/your-repo/deploy.sh && \
chmod +x deploy.sh && \
./deploy.sh
```

---

## Summary

**WHERE to run:** On your Plesk server at `/opt/gcc-webapp/`

**HOW to run:**
1. SSH to server: `ssh root@angry-pascal.70-32-82-212.plesk.page`
2. Navigate: `cd /opt/gcc-webapp`
3. Execute: `./deploy.sh`

**WHEN it works:** You'll see:
- ✅ Container started successfully!
- ✅ Health endpoint responding
- Application running on port 5102

**NEXT:** Configure Nginx in Plesk Panel and set up SSL certificate
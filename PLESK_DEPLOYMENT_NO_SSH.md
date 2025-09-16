# Deploy to Plesk WITHOUT Using SSH

## Method 1: Using Plesk Panel Terminal (Easiest)

### Step 1: Access Plesk Terminal
1. **Login to Plesk Panel**
   - Go to: https://angry-pascal.70-32-82-212.plesk.page:8443
   - Enter your Plesk username and password

2. **Open Terminal in Plesk**
   - Look for **"Terminal"** or **"Web Terminal"** in the left menu
   - OR go to **"Extensions"** → Search for **"Terminal"**
   - If not installed, install the **SSH Terminal** extension (free)

3. **You're Now in Terminal!**
   - This is like SSH but through your web browser
   - No SSH client needed!

### Step 2: Run Commands in Plesk Terminal

Copy and paste these commands one by one:

```bash
# 1. Create directory
mkdir -p /opt/gcc-webapp
cd /opt/gcc-webapp

# 2. Create the deployment script
cat > deploy.sh << 'EOF'
#!/bin/bash
echo "Starting GCC Web App Deployment..."

# Configuration
MYSQL_HOST="localhost"
MYSQL_DATABASE="gccdb"
MYSQL_USER="gccuser"
MYSQL_PASSWORD="74Fzs0YFLMXLrKJG5fzexsTkk"
CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:latest"

# Pull and run Docker container
docker pull $IMAGE_NAME
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=$MYSQL_HOST;Port=3306;Database=$MYSQL_DATABASE;User=$MYSQL_USER;Password=$MYSQL_PASSWORD;" \
  -e Jwt__Key="a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" \
  -e Jwt__Issuer="https://angry-pascal.70-32-82-212.plesk.page" \
  -e Jwt__Audience="https://angry-pascal.70-32-82-212.plesk.page" \
  $IMAGE_NAME

echo "✅ Deployment complete!"
docker ps | grep $CONTAINER_NAME
EOF

# 3. Make it executable
chmod +x deploy.sh

# 4. Run the deployment
./deploy.sh
```

---

## Method 2: Using Plesk Docker Extension (Visual Interface)

### Step 1: Install Docker Extension
1. **In Plesk Panel**, go to **Extensions**
2. Search for **"Docker"**
3. Click **"Get it Free"** or **"Install"**
4. Wait for installation

### Step 2: Use Docker Manager
1. Go to **Extensions** → **Docker**
2. Click **"Run Container"**

### Step 3: Configure Container
Fill in these fields:

**Image Name:**
```
ghcr.io/anukiai/gcc-webapp:latest
```

**Container Name:**
```
gcc-webapp
```

**Port Mapping:**
- External Port: `5102`
- Internal Port: `8080`

**Environment Variables:** Click "Add Variable" for each:
```
ASPNETCORE_ENVIRONMENT = Production
ConnectionStrings__DefaultConnection = Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;
Jwt__Key = a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047
Jwt__Issuer = https://angry-pascal.70-32-82-212.plesk.page
Jwt__Audience = https://angry-pascal.70-32-82-212.plesk.page
GCC_AZURE_AD_B2C_CLIENT_SECRET = dummy-secret
GCC_AZURE_AD_CLIENT_SECRET = dummy-secret
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING = InstrumentationKey=00000000-0000-0000-0000-000000000000
GCC_SMTP_PASSWORD = dummy-password
```

**Restart Policy:** Select `Unless Stopped`

4. Click **"Run"**

---

## Method 3: Using Plesk Scheduled Tasks (Cron)

If Terminal is not available:

### Step 1: Create Task
1. Go to **Tools & Settings** → **Scheduled Tasks (Cron Jobs)**
2. Click **"Add Task"**

### Step 2: Configure One-Time Task
**Command:**
```bash
docker pull ghcr.io/anukiai/gcc-webapp:latest && docker stop gcc-webapp 2>/dev/null; docker rm gcc-webapp 2>/dev/null; docker run -d --name gcc-webapp --restart unless-stopped -p 5102:8080 -e ASPNETCORE_ENVIRONMENT=Production -e "ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" ghcr.io/anukiai/gcc-webapp:latest
```

**Run:** Select `Now` or set a specific time

3. Click **"OK"**
4. The task will run once and deploy your container

---

## Method 4: Using Plesk File Manager + PHP Script

### Step 1: Create PHP Deployment Script
1. Go to **Files** → Navigate to your website root
2. Create new file: `deploy-gcc.php`

### Step 2: Add This Content
```php
<?php
// GCC Web App Deployment Script
header('Content-Type: text/plain');

echo "Starting GCC Web App Deployment...\n";

// Pull latest image
$output1 = shell_exec('docker pull ghcr.io/anukiai/gcc-webapp:latest 2>&1');
echo "Pull Image: " . $output1 . "\n";

// Stop existing container
$output2 = shell_exec('docker stop gcc-webapp 2>&1');
echo "Stop Container: " . $output2 . "\n";

// Remove existing container
$output3 = shell_exec('docker rm gcc-webapp 2>&1');
echo "Remove Container: " . $output3 . "\n";

// Run new container
$cmd = 'docker run -d --name gcc-webapp --restart unless-stopped -p 5102:8080 '
     . '-e ASPNETCORE_ENVIRONMENT=Production '
     . '-e "ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" '
     . '-e "Jwt__Key=a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" '
     . '-e "Jwt__Issuer=https://angry-pascal.70-32-82-212.plesk.page" '
     . '-e "Jwt__Audience=https://angry-pascal.70-32-82-212.plesk.page" '
     . 'ghcr.io/anukiai/gcc-webapp:latest 2>&1';

$output4 = shell_exec($cmd);
echo "Run Container: " . $output4 . "\n";

// Check status
$output5 = shell_exec('docker ps | grep gcc-webapp 2>&1');
echo "\nContainer Status:\n" . $output5 . "\n";

echo "\n✅ Deployment Complete!\n";
?>
```

### Step 3: Run It
- Open browser: `https://angry-pascal.70-32-82-212.plesk.page/deploy-gcc.php`
- You'll see the deployment output

### Step 4: Delete the PHP File (Security)
- After deployment, delete `deploy-gcc.php` from File Manager

---

## Which Method Should You Use?

### 🥇 **RECOMMENDED: Method 1 (Plesk Terminal)**
- Easiest and most reliable
- Just copy/paste commands
- No SSH client needed

### 🥈 **Alternative: Method 2 (Docker Extension)**
- Visual interface
- Good if you prefer clicking over typing
- Easy to manage container later

### 🥉 **Backup: Method 3 or 4**
- Use if Terminal and Docker Extension aren't available
- Method 3 (Cron) is cleaner
- Method 4 (PHP) gives immediate feedback

---

## Verify Deployment Success

### In Plesk Docker Extension:
1. Go to **Extensions** → **Docker**
2. You should see `gcc-webapp` container **Running**
3. Click on it to see logs

### Test Your Application:
1. Open browser: http://angry-pascal.70-32-82-212.plesk.page:5102
2. Should see your application
3. If not working, check container logs in Docker extension

---

## Common Issues & Fixes

### "Docker command not found"
- Install Docker through Plesk: **Extensions** → **Docker** → **Install**

### "Permission denied"
- Make sure you're logged in as admin/root user in Plesk

### "Port 5102 already in use"
- Change to another port like 5103 in the configuration

### "Cannot connect to database"
1. In Plesk go to **Databases**
2. Create database `gccdb`
3. Create user `gccuser` with password `74Fzs0YFLMXLrKJG5fzexsTkk`
4. Grant all privileges

---

## Next Steps After Container is Running

1. **Configure Nginx Proxy**
   - Go to **Websites & Domains** → Your domain
   - Click **Apache & nginx Settings**
   - Add proxy configuration

2. **Set Up SSL**
   - Go to **SSL/TLS Certificates**
   - Install Let's Encrypt certificate

3. **Test Application**
   - Visit: https://angry-pascal.70-32-82-212.plesk.page

---

## 🎯 Quick Summary

**Easiest Path:**
1. Login to Plesk Panel
2. Open Terminal (or install it from Extensions)
3. Copy/paste the deployment commands
4. Done!

**No terminal? Use:**
- Docker Extension (visual interface)
- Scheduled Task (one-time run)
- PHP script (temporary)

You don't need SSH - Plesk Panel has everything you need!
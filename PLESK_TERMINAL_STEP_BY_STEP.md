# Step-by-Step: Using Plesk Web Terminal

## Step 2: Find and Access Terminal in Plesk Panel

### Option A: If Terminal is Already Installed

1. **Look in the Left Sidebar Menu**
   - After logging into Plesk Panel
   - Scroll down the left menu
   - Look for one of these:
     - 📟 **"Terminal"**
     - 📟 **"SSH Terminal"**
     - 📟 **"Web Terminal"**
     - 📟 **"Command Line"**

2. **Click on Terminal**
   - You'll see a black screen with white text
   - It looks like this:
   ```
   [root@server ~]# _
   ```
   - This is your command prompt - you can type commands here

### Option B: If Terminal is NOT Visible - Install It

1. **Go to Extensions**
   - Click **"Extensions"** in the left menu
   - You'll see the Extensions page

2. **Search for Terminal**
   - In the search box at the top, type: `terminal`
   - OR type: `ssh`

3. **Install Terminal Extension**
   - Look for extensions named:
     - **"SSH Terminal"** (most common)
     - **"Web Terminal"**
     - **"Terminal"**
   - Click **"Get it Free"** or **"Install"**
   - Wait 30-60 seconds for installation

4. **Find the New Terminal**
   - Go back to **Extensions** menu
   - Click on **"My Extensions"** or **"Installed Extensions"**
   - Click on **"SSH Terminal"** or **"Terminal"**

### Option C: Alternative - Extensions Catalog

1. **Go to Extensions Catalog**
   - Click **"Extensions"** → **"Extension Catalog"**

2. **Browse Popular Extensions**
   - Look for **"SSH Terminal"** in the popular/featured section
   - OR use the search box

3. **Install and Access**
   - Click **"Get it Free"**
   - After installation, find it in **"My Extensions"**

## What the Terminal Screen Looks Like

When you successfully access the terminal, you'll see:

```
Welcome to Ubuntu 20.04.6 LTS (GNU/Linux 5.4.0-182-generic x86_64)

 * Documentation:  https://help.ubuntu.com
 * Management:     https://landscape.canonical.com
 * Support:        https://ubuntu.com/advantage

Last login: Sun Sep 15 10:30:45 2024 from xxx.xxx.xxx.xxx
root@server:~# _
```

**The blinking cursor after `#` is where you type commands.**

## How to Use the Terminal

### 1. **You Can Type Commands**
   - Click in the terminal window
   - Type or paste commands
   - Press **Enter** to execute

### 2. **Copy and Paste**
   - **To Paste:** Right-click → Paste (or Ctrl+Shift+V)
   - **To Copy:** Select text → Right-click → Copy

### 3. **Navigate Directories**
   ```bash
   pwd          # Shows current directory
   ls           # Lists files in current directory
   cd /opt      # Changes to /opt directory
   ```

## Now Run the Deployment Commands

Once you have the terminal open, copy and paste this **ONE COMPLETE COMMAND**:

```bash
mkdir -p /opt/gcc-webapp && cd /opt/gcc-webapp && cat > deploy.sh << 'EOF'
#!/bin/bash
echo "🚀 Starting GCC Web App Deployment..."

# Configuration
MYSQL_HOST="localhost"
MYSQL_DATABASE="gccdb"
MYSQL_USER="gccuser"
MYSQL_PASSWORD="74Fzs0YFLMXLrKJG5fzexsTkk"
CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:latest"

echo "📦 Pulling latest Docker image..."
docker pull $IMAGE_NAME

echo "🛑 Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

echo "🚀 Starting new container..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__DefaultConnection=Server=$MYSQL_HOST;Port=3306;Database=$MYSQL_DATABASE;User=$MYSQL_USER;Password=$MYSQL_PASSWORD;" \
  -e "Jwt__Key=a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" \
  -e "Jwt__Issuer=https://angry-pascal.70-32-82-212.plesk.page" \
  -e "Jwt__Audience=https://angry-pascal.70-32-82-212.plesk.page" \
  -e "GCC_AZURE_AD_B2C_CLIENT_SECRET=dummy-secret" \
  -e "GCC_AZURE_AD_CLIENT_SECRET=dummy-secret" \
  -e "GCC_APPLICATION_INSIGHTS_CONNECTION_STRING=InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e "GCC_SMTP_PASSWORD=dummy-password" \
  $IMAGE_NAME

echo "✅ Checking container status..."
sleep 5
docker ps | grep $CONTAINER_NAME

if docker ps | grep -q $CONTAINER_NAME; then
    echo ""
    echo "🎉 SUCCESS! GCC Web App is running!"
    echo "📡 Application URL: http://angry-pascal.70-32-82-212.plesk.page:5102"
    echo "❤️  Health Check: http://angry-pascal.70-32-82-212.plesk.page:5102/health"
    echo ""
    echo "Next steps:"
    echo "1. Set up nginx proxy in Plesk"
    echo "2. Configure SSL certificate"
    echo "3. Test your application"
else
    echo "❌ Container failed to start. Checking logs..."
    docker logs $CONTAINER_NAME
fi
EOF
chmod +x deploy.sh && ./deploy.sh
```

### What This Command Does:
1. **Creates directory** `/opt/gcc-webapp`
2. **Creates deployment script** with all your settings
3. **Makes it executable** and **runs it immediately**
4. **Shows status** and next steps

## Expected Output

You should see something like this:

```
🚀 Starting GCC Web App Deployment...
📦 Pulling latest Docker image...
latest: Pulling from anukiai/gcc-webapp
Already exists: [various layers]
🛑 Stopping existing container...
🚀 Starting new container...
a1b2c3d4e5f6...
✅ Checking container status...
a1b2c3d4e5f6   ghcr.io/anukiai/gcc-webapp:latest   Up 3 seconds   0.0.0.0:5102->8080/tcp

🎉 SUCCESS! GCC Web App is running!
📡 Application URL: http://angry-pascal.70-32-82-212.plesk.page:5102
❤️  Health Check: http://angry-pascal.70-32-82-212.plesk.page:5102/health
```

## If You Get Errors

### "Docker command not found"
```bash
# Install Docker first
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
```

### "Permission denied"
- Make sure you're logged in as **root** or admin user in Plesk

### "Cannot connect to Docker daemon"
```bash
# Start Docker service
systemctl start docker
systemctl enable docker
```

## Test Your Deployment

1. **Open your browser**
2. **Go to:** http://angry-pascal.70-32-82-212.plesk.page:5102
3. **You should see your GCC Web Application!**

## Still Can't Find Terminal?

### Alternative: Use Plesk's File Manager + Script

1. **Go to Files** in Plesk
2. **Create new file:** `run-deployment.sh`
3. **Paste the deployment script content**
4. **Save the file**
5. **Go to Scheduled Tasks** → Create task → Run the script

This way you can deploy without terminal!

---

**Summary:** The Plesk Web Terminal is just like having a command prompt on the server, but it runs in your web browser. No SSH client software needed!
# Docker Deployment - Complete Step-by-Step Guide for Beginners

## Prerequisites - What You Need Before Starting

### 1. Install Docker Desktop (Required)

**For Windows:**
1. Go to https://www.docker.com/products/docker-desktop
2. Click "Download for Windows"
3. Run the installer (Docker Desktop Installer.exe)
4. Follow the installation wizard
5. **IMPORTANT**: Restart your computer after installation
6. After restart, Docker Desktop should start automatically
7. You'll see the Docker whale icon in your system tray (bottom-right)

**Verify Docker is working:**
```cmd
# Open Command Prompt (cmd) or PowerShell
docker --version
# Should show: Docker version 24.x.x or similar

docker ps
# Should show: CONTAINER ID   IMAGE   COMMAND... (empty list is OK)
```

If you get an error, make sure Docker Desktop is running (check system tray).

### 2. Create a Docker Hub Account (Free)

1. Go to https://hub.docker.com/signup
2. Fill in:
   - Username (remember this - you'll need it)
   - Email
   - Password
3. Verify your email
4. Login to Docker Hub to confirm it works

### 3. Prepare Your Project

Make sure you have these files in your project folder:
- ✅ Your application code
- ✅ `Dockerfile` (already exists)
- ✅ `docker-compose.yml` (already exists)
- ✅ Build scripts (we created these)

---

## Part 1: Build Your First Docker Image Locally

### Step 1: Open Command Prompt in Your Project Folder

1. Open File Explorer
2. Navigate to: `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp`
3. In the address bar, type `cmd` and press Enter
4. Command Prompt opens in your project folder

### Step 2: Check Docker is Running

```cmd
docker ps
```

If you get an error:
1. Open Docker Desktop from Start Menu
2. Wait for it to say "Docker Desktop is running"
3. Try the command again

### Step 3: Build Your First Image (Simple Method)

Let's start with the simplest build command:

```cmd
# Build a test image
docker build -t gcc-webapp:my-first-build .
```

**What this means:**
- `docker build` = Build an image
- `-t gcc-webapp:my-first-build` = Name it "gcc-webapp" with tag "my-first-build"
- `.` = Use the current folder

**What you'll see:**
```
[+] Building 45.2s (18/18) FINISHED
 => [internal] load build definition from Dockerfile
 => [internal] load .dockerignore
 => [base 1/2] FROM mcr.microsoft.com/dotnet/aspnet:8.0
 => [build 1/7] FROM mcr.microsoft.com/dotnet/sdk:8.0
 ... (lots of output)
 => exporting to image
 => => naming to docker.io/library/gcc-webapp:my-first-build
```

This takes 2-10 minutes first time (downloading .NET images).

### Step 4: Verify Your Image Was Built

```cmd
docker images
```

You should see:
```
REPOSITORY    TAG              IMAGE ID       CREATED         SIZE
gcc-webapp    my-first-build   abc123def456   2 minutes ago   250MB
```

🎉 **Congratulations! You've built your first Docker image!**

### Step 5: Test Your Image Locally

```cmd
# Run the container
docker run -d -p 5000:8080 --name test-container gcc-webapp:my-first-build
```

**What this means:**
- `docker run` = Start a container
- `-d` = Run in background
- `-p 5000:8080` = Map port 5000 on your PC to port 8080 in container
- `--name test-container` = Name it "test-container"
- `gcc-webapp:my-first-build` = Use the image you built

**Test it:**
1. Open browser
2. Go to: http://localhost:5000
3. You should see your application!

**Stop the test:**
```cmd
docker stop test-container
docker rm test-container
```

---

## Part 2: Use the Build Script (Easier Method)

### Step 1: Use the Automated Build Script

Instead of typing commands, use the script we created:

```cmd
docker-build.cmd
```

You'll see a menu:
```
============================================================
   Docker Image Build Menu
============================================================

1. Build Development Image
2. Build Test Image
3. Build Production Image
...

Select build option:
```

### Step 2: Build a Test Image

1. Type `2` and press Enter (Build Test Image)
2. The script will:
   - Build the image
   - Tag it properly
   - Run a health check
   - Show you the result

---

## Part 3: Push to Docker Hub (Share Your Image)

### Step 1: Login to Docker Hub

```cmd
docker login
```

Enter:
- Username: (your Docker Hub username)
- Password: (your Docker Hub password)

You should see: `Login Succeeded`

### Step 2: Tag Your Image for Docker Hub

Your image needs to include your Docker Hub username:

```cmd
# Format: docker tag LOCAL_IMAGE DOCKERHUB_USERNAME/IMAGE_NAME:TAG
docker tag gcc-webapp:my-first-build YOUR_USERNAME/gcc-webapp:latest
```

Replace `YOUR_USERNAME` with your actual Docker Hub username.

### Step 3: Push to Docker Hub

```cmd
docker push YOUR_USERNAME/gcc-webapp:latest
```

**What you'll see:**
```
The push refers to repository [docker.io/YOUR_USERNAME/gcc-webapp]
abc123: Pushing [=========>                ] 10.24MB/45.3MB
def456: Pushed
latest: digest: sha256:1234567890abcdef size: 2847
```

### Step 4: Verify on Docker Hub

1. Go to https://hub.docker.com
2. Login
3. You should see your repository: `YOUR_USERNAME/gcc-webapp`
4. Click on it to see details

---

## Part 4: Deploy to Your Plesk Server

### Step 1: Prepare Your Server

Connect to your Plesk server (via SSH or Plesk SSH Terminal):

```bash
# Check if Docker is installed
docker --version

# If not installed, install it:
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
```

### Step 2: Pull Your Image on the Server

On your Plesk server:

```bash
# Login to Docker Hub
docker login
# Enter your credentials

# Pull your image
docker pull YOUR_USERNAME/gcc-webapp:latest
```

### Step 3: Create a Simple Start Script

Create a file called `start-gcc.sh`:

```bash
#!/bin/bash

# Stop any existing container
docker stop gcc-webapp 2>/dev/null
docker rm gcc-webapp 2>/dev/null

# Start the container
docker run -d \
  --name gcc-webapp \
  --restart always \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  YOUR_USERNAME/gcc-webapp:latest

echo "GCC Web App started on port 5102"
```

Make it executable:
```bash
chmod +x start-gcc.sh
```

### Step 4: Run Your Application

```bash
./start-gcc.sh
```

### Step 5: Configure Plesk to Proxy to Docker

In Plesk Panel:

1. Go to your domain
2. Click "Apache & nginx Settings"
3. In "Additional nginx directives", add:

```nginx
location / {
    proxy_pass http://localhost:5102;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection 'upgrade';
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
}
```

4. Click "Apply"

Your app should now be accessible at your domain!

---

## Part 5: Using Docker Compose (Complete Stack)

### Step 1: Prepare Environment File

Create `.env.production`:

```env
# Database password
DB_SA_PASSWORD=YourStrong@Password123

# JWT Secret (make this long and random)
JWT_SECRET_KEY=your-super-secret-key-at-least-64-characters-long-change-this

# Redis password
REDIS_PASSWORD=YourRedis@Password123
```

### Step 2: Start Everything with Docker Compose

```bash
# Start all services (app, database, redis)
docker-compose -f docker-compose.production.yml up -d
```

This starts:
- Your web application
- SQL Server database
- Redis cache

### Step 3: Check Everything is Running

```bash
docker-compose ps
```

You should see:
```
NAME                 STATUS    PORTS
gcc-webapp-prod      Up        0.0.0.0:5102->8080/tcp
gcc-sqlserver-prod   Up        127.0.0.1:1433->1433/tcp
gcc-redis-prod       Up        127.0.0.1:6379->6379/tcp
```

---

## Common Problems and Solutions

### Problem 1: "Docker daemon is not running"

**Solution:**
1. Start Docker Desktop from Start Menu
2. Wait for "Docker Desktop is running"
3. Try command again

### Problem 2: "Port already in use"

**Solution:**
```cmd
# Find what's using the port
netstat -ano | findstr :5102

# Change the port in docker run command
docker run -p 5103:8080 ...  # Use 5103 instead
```

### Problem 3: "Cannot connect to Docker daemon"

**Solution:**
1. Right-click Docker Desktop icon
2. Select "Restart"
3. Wait 1-2 minutes
4. Try again

### Problem 4: Build fails with "file not found"

**Solution:**
Make sure you're in the right folder:
```cmd
# Should show your project files
dir

# Should see Dockerfile
dir Dockerfile
```

### Problem 5: "Login failed" to Docker Hub

**Solution:**
1. Reset password at https://hub.docker.com
2. Try login again
3. Or create access token:
   - Go to Docker Hub → Account Settings → Security
   - New Access Token
   - Use token as password

---

## Quick Command Reference

### Essential Docker Commands

```cmd
# Build image
docker build -t gcc-webapp:latest .

# List images
docker images

# Run container
docker run -d -p 5102:8080 gcc-webapp:latest

# List running containers
docker ps

# Stop container
docker stop CONTAINER_NAME

# Remove container
docker rm CONTAINER_NAME

# View logs
docker logs CONTAINER_NAME

# Enter container shell
docker exec -it CONTAINER_NAME /bin/bash

# Clean up everything
docker system prune -a
```

### Docker Compose Commands

```cmd
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs

# Restart a service
docker-compose restart webapp

# Pull latest images
docker-compose pull
```

---

## Your Deployment Checklist

### Local Development
- [ ] Docker Desktop installed and running
- [ ] Build image locally: `docker build -t gcc-webapp:test .`
- [ ] Test locally: `docker run -p 5000:8080 gcc-webapp:test`
- [ ] Verify app works: http://localhost:5000

### Push to Registry
- [ ] Create Docker Hub account
- [ ] Login: `docker login`
- [ ] Tag image: `docker tag gcc-webapp:test USERNAME/gcc-webapp:latest`
- [ ] Push: `docker push USERNAME/gcc-webapp:latest`

### Deploy to Server
- [ ] SSH to server
- [ ] Install Docker
- [ ] Pull image: `docker pull USERNAME/gcc-webapp:latest`
- [ ] Create start script
- [ ] Run container
- [ ] Configure Plesk nginx proxy
- [ ] Test production URL

### Verify Deployment
- [ ] Application accessible
- [ ] Database connected
- [ ] Login works
- [ ] Basic features work
- [ ] SSL certificate active

---

## Getting Help

### If something doesn't work:

1. **Check Docker is running:**
   - Look for whale icon in system tray
   - Should say "Docker Desktop is running"

2. **Check the logs:**
   ```cmd
   docker logs CONTAINER_NAME
   ```

3. **Google the error message**
   - Most Docker errors are common
   - Stack Overflow usually has answers

4. **Ask for help with these details:**
   - The command you ran
   - The error message
   - Output of `docker version`
   - Output of `docker ps`

### Useful Resources

- Docker Documentation: https://docs.docker.com/get-started/
- Docker Hub: https://hub.docker.com/
- Docker Desktop Troubleshooting: https://docs.docker.com/desktop/troubleshoot/overview/

---

## Next Steps

Once you're comfortable with the basics:

1. **Automate with scripts**
   - Use `docker-build.cmd` for building
   - Use `docker-push.cmd` for pushing

2. **Set up CI/CD**
   - GitHub Actions will build automatically
   - Pushes to registry on every commit

3. **Add monitoring**
   - Health checks
   - Log aggregation
   - Performance monitoring

4. **Implement backups**
   - Database backups
   - Volume backups
   - Disaster recovery plan

---

**Remember:** Everyone starts somewhere. Docker seems complex at first, but once you understand the basics (build → push → pull → run), everything else builds on that foundation.

**Pro Tip:** Keep this guide handy and work through it step by step. Don't skip steps even if they seem obvious. Each successful step builds confidence!

---

*Good luck with your Docker deployment! You've got this! 🚀*
# No SSH Access - Plesk Deployment Guide

## Method 1: Plesk File Manager + Docker Extension

### Step 1: Install Docker Extension
1. Login to Plesk Panel
2. Go to **Extensions** → **Extension Catalog**
3. Search for "Docker" and install the Docker extension

### Step 2: Upload Files via File Manager
1. Go to **Files** → **File Manager**
2. Create directory: `/opt/gcc-webapp/`
3. Upload these files:
   - `docker-deploy-plesk-registry-MARIADB.cmd`
   - `.env.plesk-UPDATED` (rename to `.env`)
   - `plesk-nginx-config.conf`

### Step 3: Use Plesk Docker Extension
1. Go to **Extensions** → **Docker**
2. Pull image: `ghcr.io/anukiai/gcc-webapp:latest`
3. Create container with these settings:
   - **Name**: gcc-webapp
   - **Image**: ghcr.io/anukiai/gcc-webapp:latest
   - **Port mapping**: 5102:8080
   - **Environment variables**:
     ```
     ASPNETCORE_ENVIRONMENT=Production
     ASPNETCORE_URLS=http://0.0.0.0:8080
     ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;
     Jwt__Key=a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047
     Jwt__Issuer=https://angry-pascal.70-32-82-212.plesk.page
     Jwt__Audience=https://angry-pascal.70-32-82-212.plesk.page
     ```

## Method 2: Plesk Scheduled Tasks

### Step 1: Create Deployment Script
Create file `/opt/gcc-webapp/deploy-via-cron.sh` with content:

```bash
#!/bin/bash
cd /opt/gcc-webapp
echo "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" | docker login ghcr.io -u AnukiAI --password-stdin
docker pull ghcr.io/anukiai/gcc-webapp:latest
docker stop gcc-webapp 2>/dev/null
docker rm gcc-webapp 2>/dev/null
docker run -d --name gcc-webapp --restart unless-stopped -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://0.0.0.0:8080" \
  -e ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" \
  -e Jwt__Key="a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" \
  -e Jwt__Issuer="https://angry-pascal.70-32-82-212.plesk.page" \
  -e Jwt__Audience="https://angry-pascal.70-32-82-212.plesk.page" \
  ghcr.io/anukiai/gcc-webapp:latest
```

### Step 2: Run via Scheduled Task
1. Go to **Tools & Settings** → **Scheduled Tasks**
2. Create new task:
   - **Command**: `/bin/bash /opt/gcc-webapp/deploy-via-cron.sh`
   - **Run**: Once, immediately

## Method 3: Contact Hosting Provider

If neither method works:
1. Contact your hosting provider
2. Request SSH access or Docker deployment assistance
3. Provide them with the deployment commands
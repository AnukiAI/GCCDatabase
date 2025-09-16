# How to Paste in Plesk Terminal - Multiple Solutions

## Problem: Can't Paste Script in Terminal

### Solution 1: Different Paste Methods

Try these paste methods in order:

1. **Right-click → Paste**
   - Copy the script text
   - Right-click in the terminal window
   - Select "Paste"

2. **Ctrl+Shift+V** (Linux terminal standard)
   - Copy the script
   - Press `Ctrl+Shift+V` (not Ctrl+V)

3. **Middle Mouse Button**
   - Select/copy the text
   - Click middle mouse button in terminal

4. **Browser Menu**
   - Copy the script
   - Use browser's Edit → Paste menu

### Solution 2: Type Commands One by One

Instead of pasting the big script, type these commands **one at a time**:

```bash
mkdir -p /opt/gcc-webapp
```
Press Enter, then:

```bash
cd /opt/gcc-webapp
```
Press Enter, then:

```bash
docker pull ghcr.io/anukiai/gcc-webapp:latest
```
Press Enter, then:

```bash
docker stop gcc-webapp
```
Press Enter, then:

```bash
docker rm gcc-webapp
```
Press Enter, then this long command (copy line by line if needed):

```bash
docker run -d --name gcc-webapp --restart unless-stopped -p 5102:8080 -e ASPNETCORE_ENVIRONMENT=Production -e "ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" ghcr.io/anukiai/gcc-webapp:latest
```

### Solution 3: Use Plesk File Manager Instead

**Much Easier - No Terminal Needed!**

1. **Go to Files** in Plesk Panel
2. **Navigate to root directory** (/)
3. **Create folder:** `opt` (if not exists)
4. **Go into opt folder**
5. **Create folder:** `gcc-webapp`
6. **Go into gcc-webapp folder**
7. **Click "Create" → "File"**
8. **Name it:** `deploy.sh`
9. **Paste this content in the file:**

```bash
#!/bin/bash
echo "Starting GCC Web App Deployment..."

# Pull latest image
docker pull ghcr.io/anukiai/gcc-webapp:latest

# Stop existing container
docker stop gcc-webapp 2>/dev/null
docker rm gcc-webapp 2>/dev/null

# Run new container
docker run -d \
  --name gcc-webapp \
  --restart unless-stopped \
  -p 5102:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" \
  -e "Jwt__Key=a56257adb423a4c132e969faf96ecc3aeab11dbef571d95caf818144e6618047" \
  -e "Jwt__Issuer=https://angry-pascal.70-32-82-212.plesk.page" \
  -e "Jwt__Audience=https://angry-pascal.70-32-82-212.plesk.page" \
  ghcr.io/anukiai/gcc-webapp:latest

echo "Deployment complete!"
docker ps | grep gcc-webapp
```

10. **Save the file**
11. **Now in terminal, just type:**
    ```bash
    cd /opt/gcc-webapp
    chmod +x deploy.sh
    ./deploy.sh
    ```

### Solution 4: Use Scheduled Tasks (No Terminal at All)

1. **Go to Tools & Settings** → **Scheduled Tasks**
2. **Add Task**
3. **Command:**
   ```bash
   /bin/bash -c "mkdir -p /opt/gcc-webapp && cd /opt/gcc-webapp && docker pull ghcr.io/anukiai/gcc-webapp:latest && docker stop gcc-webapp 2>/dev/null && docker rm gcc-webapp 2>/dev/null && docker run -d --name gcc-webapp --restart unless-stopped -p 5102:8080 -e ASPNETCORE_ENVIRONMENT=Production -e 'ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;' ghcr.io/anukiai/gcc-webapp:latest"
   ```
4. **Run:** Once, Now
5. **Click OK**

### Solution 5: Simple Docker Run (Shortest)

If nothing else works, just type this ONE command in terminal:

```bash
docker run -d --name gcc-webapp --restart unless-stopped -p 5102:8080 -e ASPNETCORE_ENVIRONMENT=Production -e "ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;" ghcr.io/anukiai/gcc-webapp:latest
```

## Why Paste Might Not Work

- **Security restrictions** in web-based terminals
- **Browser blocking** clipboard access
- **Different terminal emulator** requirements
- **Linux vs Windows** clipboard differences

## Recommended Approach: File Manager Method (Solution 3)

This is the **easiest and most reliable**:
1. Create the script file using Plesk File Manager
2. Use terminal only to run it: `./deploy.sh`
3. No copy/paste issues!

## Check if Deployment Worked

After running any of these solutions:

1. **Check container status:**
   ```bash
   docker ps
   ```

2. **Test your app:**
   Open browser: http://angry-pascal.70-32-82-212.plesk.page:5102

3. **Check logs if problems:**
   ```bash
   docker logs gcc-webapp
   ```

## Still Having Issues?

**Try the Plesk Docker Extension:**
1. Go to **Extensions** → Install **Docker**
2. Use the visual interface to create your container
3. No command line needed at all!

**Container Settings for Docker Extension:**
- **Image:** `ghcr.io/anukiai/gcc-webapp:latest`
- **Name:** `gcc-webapp`
- **Port:** `5102:8080`
- **Environment:** Add the connection string and other variables

---

**Bottom Line:** If terminal paste doesn't work, use **File Manager** to create the script, then run it from terminal. Much more reliable!
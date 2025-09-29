#!/bin/bash
# GCC Web App - Plesk .NET 8.0 Deployment Script
# Automatically deploys the application to Plesk server using .NET Toolkit

set -e  # Exit on any error

# Configuration - MODIFY THESE VALUES
PLESK_SERVER="your-plesk-server.com"
PLESK_USER="your-plesk-user"
DOMAIN_NAME="yourdomain.com"
DOMAIN_PATH="/var/www/vhosts/$DOMAIN_NAME"
DEPLOYMENT_PACKAGE="gccwebapp-dotnet8.tar.gz"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}   GCC Web App - Plesk Deployment${NC}"
echo -e "${BLUE}   .NET 8.0 with Plesk .NET Toolkit${NC}"
echo -e "${BLUE}========================================${NC}"

# Verify deployment package exists
if [ ! -f "$DEPLOYMENT_PACKAGE" ]; then
    echo -e "${RED}Error: Deployment package '$DEPLOYMENT_PACKAGE' not found!${NC}"
    echo "Please build the application first with: dotnet publish GCCWebApp.csproj -c Release -r linux-x64 --self-contained false -o ./publish"
    exit 1
fi

echo -e "${GREEN}✓ Deployment package found: $DEPLOYMENT_PACKAGE ($(ls -lh $DEPLOYMENT_PACKAGE | awk '{print $5}'))${NC}"

# Check server configuration
echo -e "\n${YELLOW}Deployment Configuration:${NC}"
echo "Server: $PLESK_SERVER"
echo "User: $PLESK_USER"
echo "Domain: $DOMAIN_NAME"
echo "Path: $DOMAIN_PATH"

read -p "Continue with deployment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Deployment cancelled."
    exit 0
fi

echo -e "\n${BLUE}Step 1: Uploading deployment package...${NC}"
scp "$DEPLOYMENT_PACKAGE" "$PLESK_USER@$PLESK_SERVER:$DOMAIN_PATH/" || {
    echo -e "${RED}Failed to upload deployment package${NC}"
    exit 1
}
echo -e "${GREEN}✓ Package uploaded successfully${NC}"

echo -e "\n${BLUE}Step 2: Deploying application on server...${NC}"
ssh "$PLESK_USER@$PLESK_SERVER" << ENDSSH || {
    echo -e "${RED}Failed to deploy on server${NC}"
    exit 1
}

set -e
echo "Navigating to domain directory..."
cd "$DOMAIN_PATH/httpdocs" || exit 1

echo "Creating backup of existing application..."
if [ -d "backup" ]; then
    rm -rf backup_old
    mv backup backup_old
fi
mkdir -p backup
if [ -f GCCWebApp.dll ]; then
    cp -r * backup/ 2>/dev/null || true
    echo "✓ Backup created"
else
    echo "! No existing application found, fresh deployment"
fi

echo "Extracting new application..."
tar -xzf "../$DEPLOYMENT_PACKAGE" || exit 1
echo "✓ Application extracted"

echo "Setting correct permissions..."
chown -R psacln:psaserv . || {
    echo "Warning: Could not set psacln:psaserv ownership, trying current user..."
    chown -R \$(whoami):\$(whoami) . || echo "Warning: Permission setting failed"
}
chmod 755 . || echo "Warning: chmod failed"
chmod +x GCCWebApp || echo "Warning: GCCWebApp executable not found or chmod failed"
echo "✓ Permissions set"

echo "Verifying deployment files..."
if [ -f "GCCWebApp.dll" ]; then
    echo "✓ GCCWebApp.dll found"
else
    echo "✗ GCCWebApp.dll missing!"
    exit 1
fi

if [ -f "appsettings.json" ]; then
    echo "✓ appsettings.json found"
else
    echo "✗ appsettings.json missing!"
    exit 1
fi

if [ -d "wwwroot" ]; then
    echo "✓ wwwroot directory found"
else
    echo "✗ wwwroot directory missing!"
    exit 1
fi

echo "✓ All essential files verified"

echo "Cleaning up deployment package..."
rm -f "../$DEPLOYMENT_PACKAGE"
echo "✓ Cleanup complete"

ENDSSH

echo -e "${GREEN}✓ Server deployment completed${NC}"

echo -e "\n${BLUE}Step 3: Configuring .NET Toolkit in Plesk...${NC}"
echo -e "${YELLOW}Manual configuration required in Plesk Control Panel:${NC}"
echo "1. Go to Websites & Domains → $DOMAIN_NAME"
echo "2. Click '.NET Toolkit'"
echo "3. Configure the following settings:"
echo "   - Startup File: GCCWebApp.dll"
echo "   - ASP.NET Core Version: 8.0"
echo "   - Environment: Production"
echo "   - Port: (auto-assigned by Plesk)"
echo ""
echo "4. Set Environment Variables:"
echo "   ASPNETCORE_ENVIRONMENT=Production"
echo "   ConnectionStrings__DefaultConnection=your_production_connection_string"
echo "   GCC_JWT_SECRET=your_jwt_secret_min_64_chars"
echo ""
echo "5. Click 'Start' to launch the application"

echo -e "\n${BLUE}Step 4: SSL Configuration (Recommended)${NC}"
echo "1. Go to SSL/TLS Certificates in Plesk"
echo "2. Install Let's Encrypt certificate (free)"
echo "3. Enable 'Permanent SEO-safe 301 redirect from HTTP to HTTPS'"

echo -e "\n${BLUE}Step 5: Verification${NC}"
echo "After completing the Plesk configuration:"
echo "• Check application status in .NET Toolkit"
echo "• View application logs for any errors"
echo "• Test the application in your browser"
echo "• Verify API endpoints are working"

echo -e "\n${BLUE}Troubleshooting Commands:${NC}"
echo "SSH into server and run these if needed:"
echo "# Check application status"
echo "systemctl status plesk-dotnet-$DOMAIN_NAME"
echo ""
echo "# View application logs"
echo "journalctl -u plesk-dotnet-$DOMAIN_NAME -f"
echo ""
echo "# Restart application via Plesk CLI"
echo "plesk bin site --update $DOMAIN_NAME -dotnet-restart"

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}   Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Complete manual Plesk .NET Toolkit configuration"
echo "2. Set up production database connection"
echo "3. Configure environment variables"
echo "4. Test application functionality"
echo "5. Set up SSL certificate"
echo ""
echo -e "${BLUE}For detailed instructions, see: PLESK_DOTNET8_DEPLOYMENT.md${NC}"
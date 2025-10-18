#!/bin/bash

# =================================================================
# GCC Web Application - Native Deployment Script for Plesk
# =================================================================
# This script deploys the ASP.NET Core MVC application natively
# on a Plesk server without Docker
# =================================================================

set -e  # Exit on error

# Configuration variables - CHANGE THESE FOR YOUR ENVIRONMENT
DOMAIN="yourdomain.com"
APP_PATH="/var/www/vhosts/$DOMAIN/httpdocs"
SERVICE_NAME="gcc-webapp"
DB_TYPE="sqlite"  # Options: sqlite, postgresql, mariadb
PORT="5000"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_section() {
    echo -e "\n${BLUE}=== $1 ===${NC}"
}

# Check if running as root
check_root() {
    if [[ $EUID -ne 0 ]]; then
       print_error "This script must be run as root (use sudo)"
       exit 1
    fi
}

# Create required directories
create_directories() {
    print_section "Creating Directory Structure"

    mkdir -p "$APP_PATH"
    mkdir -p "$APP_PATH/App_Data"
    mkdir -p "$APP_PATH/logs"
    mkdir -p "$APP_PATH/DataProtectionKeys"
    mkdir -p "$APP_PATH/wwwroot/uploads"

    print_status "Directories created successfully"
}

# Set proper permissions
set_permissions() {
    print_section "Setting Permissions"

    # Set ownership to Plesk user
    chown -R plesk:psacln "$APP_PATH"

    # Set directory permissions
    chmod 755 "$APP_PATH"
    chmod 755 "$APP_PATH/App_Data"
    chmod 755 "$APP_PATH/logs"
    chmod 755 "$APP_PATH/DataProtectionKeys"
    chmod 777 "$APP_PATH/wwwroot/uploads"

    print_status "Permissions set successfully"
}

# Configure database
configure_database() {
    print_section "Database Configuration"

    case $DB_TYPE in
        sqlite)
            print_status "Using SQLite database"
            # SQLite database will be created automatically
            DB_CONNECTION="Data Source=App_Data/gcc_database.db"
            ;;

        postgresql)
            print_status "Configuring PostgreSQL database"
            read -p "Enter PostgreSQL host [localhost]: " PG_HOST
            PG_HOST=${PG_HOST:-localhost}
            read -p "Enter PostgreSQL database name [gccdb]: " PG_DB
            PG_DB=${PG_DB:-gccdb}
            read -p "Enter PostgreSQL username [gccuser]: " PG_USER
            PG_USER=${PG_USER:-gccuser}
            read -s -p "Enter PostgreSQL password: " PG_PASS
            echo
            DB_CONNECTION="Host=$PG_HOST;Database=$PG_DB;Username=$PG_USER;Password=$PG_PASS"
            ;;

        mariadb)
            print_status "Configuring MariaDB/MySQL database"
            read -p "Enter MariaDB host [localhost]: " MYSQL_HOST
            MYSQL_HOST=${MYSQL_HOST:-localhost}
            read -p "Enter MariaDB database name [gccdb]: " MYSQL_DB
            MYSQL_DB=${MYSQL_DB:-gccdb}
            read -p "Enter MariaDB username [gccuser]: " MYSQL_USER
            MYSQL_USER=${MYSQL_USER:-gccuser}
            read -s -p "Enter MariaDB password: " MYSQL_PASS
            echo
            DB_CONNECTION="Server=$MYSQL_HOST;Database=$MYSQL_DB;User=$MYSQL_USER;Password=$MYSQL_PASS"
            ;;

        *)
            print_error "Invalid database type: $DB_TYPE"
            exit 1
            ;;
    esac
}

# Generate JWT secret key
generate_jwt_key() {
    print_section "Generating JWT Secret Key"
    JWT_KEY=$(openssl rand -base64 48)
    print_status "JWT key generated successfully"
}

# Create appsettings.Production.json
create_appsettings() {
    print_section "Creating Application Settings"

    cat > "$APP_PATH/appsettings.Production.json" << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "$DB_CONNECTION"
  },
  "Jwt": {
    "Key": "$JWT_KEY",
    "Issuer": "https://$DOMAIN",
    "Audience": "https://$DOMAIN",
    "ExpiryInMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "$DOMAIN;www.$DOMAIN",
  "Features": {
    "EnableDetailedErrorMessages": false,
    "RequireHttpsForAllRequests": true,
    "EnableSwagger": true
  },
  "ApplicationSettings": {
    "SiteUrl": "https://$DOMAIN",
    "AdminEmail": "admin@$DOMAIN"
  }
}
EOF

    chmod 640 "$APP_PATH/appsettings.Production.json"
    chown plesk:psacln "$APP_PATH/appsettings.Production.json"

    print_status "Application settings created successfully"
}

# Create systemd service
create_systemd_service() {
    print_section "Creating Systemd Service"

    # Copy service file
    cp gcc-webapp.service /etc/systemd/system/

    # Update service file with actual paths
    sed -i "s|yourdomain.com|$DOMAIN|g" /etc/systemd/system/gcc-webapp.service
    sed -i "s|localhost:5000|localhost:$PORT|g" /etc/systemd/system/gcc-webapp.service

    # Reload systemd
    systemctl daemon-reload

    print_status "Systemd service created successfully"
}

# Build and publish application
build_application() {
    print_section "Building and Publishing Application"

    cd "$APP_PATH"

    # Check if source files exist
    if [ -f "GCCWebApp.csproj" ]; then
        print_status "Building from source..."

        # Restore packages
        print_status "Restoring NuGet packages..."
        dotnet restore

        # Build in Release mode
        print_status "Building application..."
        dotnet build -c Release

        # Publish application
        print_status "Publishing application..."
        dotnet publish -c Release -o .

        # Run database migrations
        print_status "Running database migrations..."
        dotnet ef database update || print_warning "Database migration failed - may need to run manually"

    else
        print_warning "Source files not found. Please upload published files to $APP_PATH"
        read -p "Have you uploaded the published files? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_error "Please upload the published application files and run this script again"
            exit 1
        fi
    fi
}

# Start and enable service
start_service() {
    print_section "Starting Application Service"

    # Enable service
    systemctl enable gcc-webapp

    # Start service
    systemctl start gcc-webapp

    # Wait for service to start
    sleep 5

    # Check status
    if systemctl is-active --quiet gcc-webapp; then
        print_status "✅ Application service is running"
        systemctl status gcc-webapp --no-pager
    else
        print_error "❌ Application service failed to start"
        journalctl -u gcc-webapp -n 50 --no-pager
        exit 1
    fi
}

# Test application
test_application() {
    print_section "Testing Application"

    # Test local endpoint
    print_status "Testing local endpoint..."
    if curl -s -o /dev/null -w "%{http_code}" http://localhost:$PORT/health | grep -q "200"; then
        print_status "✅ Application is responding on port $PORT"
    else
        print_warning "⚠️ Application health check failed"
    fi

    # Test main page
    if curl -s -o /dev/null -w "%{http_code}" http://localhost:$PORT | grep -q "200\|302"; then
        print_status "✅ Main page is accessible"
    else
        print_warning "⚠️ Main page not accessible"
    fi
}

# Display next steps
show_next_steps() {
    print_section "Deployment Complete!"

    echo -e "\n${GREEN}✅ Application deployed successfully!${NC}\n"

    echo "Next steps:"
    echo "1. Configure Nginx in Plesk panel:"
    echo "   - Go to Domain → Apache & nginx Settings"
    echo "   - Add contents of nginx-plesk-config.conf to 'Additional nginx directives'"
    echo ""
    echo "2. Configure SSL certificate:"
    echo "   - Go to Domain → SSL/TLS Certificates"
    echo "   - Install Let's Encrypt certificate"
    echo ""
    echo "3. Update nginx-plesk-config.conf:"
    echo "   - Replace 'yourdomain.com' with '$DOMAIN'"
    echo "   - Replace 'localhost:5000' with 'localhost:$PORT' if using different port"
    echo ""
    echo "4. Test your application:"
    echo "   - https://$DOMAIN"
    echo "   - https://$DOMAIN/swagger (API documentation)"
    echo "   - https://$DOMAIN/health (Health check)"
    echo ""
    echo "Useful commands:"
    echo "  systemctl status gcc-webapp    # Check service status"
    echo "  journalctl -u gcc-webapp -f    # View logs"
    echo "  systemctl restart gcc-webapp   # Restart application"
    echo ""
}

# Main execution
main() {
    print_section "GCC Web Application Native Deployment"

    # Check prerequisites
    check_root

    # Check if .NET is installed
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please run install-dotnet-plesk.sh first"
        exit 1
    fi

    # Get domain if not set
    if [ "$DOMAIN" == "yourdomain.com" ]; then
        read -p "Enter your domain name: " DOMAIN
    fi

    # Execute deployment steps
    create_directories
    set_permissions
    configure_database
    generate_jwt_key
    create_appsettings
    create_systemd_service
    build_application
    start_service
    test_application
    show_next_steps
}

# Handle script interruption
trap 'print_error "Deployment interrupted"; exit 1' INT TERM

# Run main function
main "$@"
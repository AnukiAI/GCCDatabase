#!/bin/bash

# =================================================================
# .NET 6.0 Runtime Installation Script for Plesk Server
# =================================================================
# Simplified script specifically for .NET 6.0 installation
# =================================================================

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_status() { echo -e "${GREEN}[INFO]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }

# Check root
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root (use sudo)"
   exit 1
fi

print_status "=== Installing .NET 6.0 for Plesk Server ==="

# Detect OS
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
    VER=$VERSION_ID
fi

print_status "Detected OS: $OS $VER"

# Method 1: Try package manager first
print_status "Method 1: Using package manager..."

if [[ "$OS" == "ubuntu" ]] || [[ "$OS" == "debian" ]]; then
    # Add Microsoft package repository
    wget -q https://packages.microsoft.com/config/$OS/$VER/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb

    apt-get update
    apt-get install -y dotnet-sdk-6.0 aspnetcore-runtime-6.0

elif [[ "$OS" == "centos" ]] || [[ "$OS" == "rhel" ]] || [[ "$OS" == "almalinux" ]] || [[ "$OS" == "rocky" ]]; then
    # Add Microsoft repository
    rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm

    yum install -y dotnet-sdk-6.0 aspnetcore-runtime-6.0
fi

# Method 2: If package manager fails, use install script
if ! command -v dotnet &> /dev/null; then
    print_warning "Package manager installation failed. Using install script..."

    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh

    # Install .NET 6.0
    ./dotnet-install.sh --channel 6.0 --install-dir /usr/share/dotnet
    ./dotnet-install.sh --channel 6.0 --runtime aspnetcore --install-dir /usr/share/dotnet

    # Create symlink
    ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

    rm dotnet-install.sh
fi

# Configure environment
cat > /etc/profile.d/dotnet.sh << 'EOF'
export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT
export DOTNET_CLI_TELEMETRY_OPTOUT=1
EOF

chmod +x /etc/profile.d/dotnet.sh
source /etc/profile.d/dotnet.sh

# Install Entity Framework tools
dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"

# Verify installation
print_status "Verifying installation..."
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes

if dotnet --list-runtimes | grep -q "Microsoft.AspNetCore.App 6.0"; then
    print_status "✅ ASP.NET Core 6.0 runtime installed successfully!"
else
    print_error "❌ ASP.NET Core 6.0 runtime not found"
    exit 1
fi

print_status "=== .NET 6.0 Installation Complete ==="
print_status ""
print_status "Next steps:"
print_status "1. Upload your application to /var/www/vhosts/yourdomain.com/httpdocs"
print_status "2. Run: dotnet restore"
print_status "3. Run: dotnet publish -c Release"
print_status "4. Configure systemd service"
print_status "5. Configure Nginx in Plesk"
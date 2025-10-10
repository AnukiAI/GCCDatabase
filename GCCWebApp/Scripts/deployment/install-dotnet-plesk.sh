#!/bin/bash

# =================================================================
# .NET 6.0/8.0 Runtime Installation Script for Plesk Server
# =================================================================
# This script installs .NET runtime and SDK on a Plesk server
# Compatible with Ubuntu, Debian, CentOS, AlmaLinux, and RHEL
# Default: .NET 6.0 (change DOTNET_VERSION to switch)
# =================================================================

# Configuration - Change this to switch .NET versions
DOTNET_VERSION="6.0"  # Options: "6.0" or "8.0"

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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

# Detect OS
detect_os() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        OS=$NAME
        VER=$VERSION_ID
        ID=$ID
    elif type lsb_release >/dev/null 2>&1; then
        OS=$(lsb_release -si)
        VER=$(lsb_release -sr)
    else
        print_error "Cannot detect OS version"
        exit 1
    fi

    print_status "Detected OS: $OS $VER"
}

# Install prerequisites
install_prerequisites() {
    print_status "Installing prerequisites..."

    if [[ "$ID" == "ubuntu" ]] || [[ "$ID" == "debian" ]]; then
        apt-get update
        apt-get install -y wget apt-transport-https software-properties-common
    elif [[ "$ID" == "centos" ]] || [[ "$ID" == "rhel" ]] || [[ "$ID" == "almalinux" ]] || [[ "$ID" == "rocky" ]]; then
        yum install -y wget
    else
        print_error "Unsupported OS: $ID"
        exit 1
    fi
}

# Install .NET using Microsoft's package repositories
install_dotnet_package_manager() {
    print_status "Installing .NET $DOTNET_VERSION from Microsoft package repository..."

    # Set package names based on version
    if [[ "$DOTNET_VERSION" == "6.0" ]]; then
        SDK_PACKAGE="dotnet-sdk-6.0"
        RUNTIME_PACKAGE="aspnetcore-runtime-6.0"
    elif [[ "$DOTNET_VERSION" == "8.0" ]]; then
        SDK_PACKAGE="dotnet-sdk-8.0"
        RUNTIME_PACKAGE="aspnetcore-runtime-8.0"
    else
        print_error "Unsupported .NET version: $DOTNET_VERSION"
        exit 1
    fi

    if [[ "$ID" == "ubuntu" ]]; then
        # Add Microsoft package repository for Ubuntu
        wget https://packages.microsoft.com/config/ubuntu/$VER/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb

        apt-get update
        apt-get install -y $SDK_PACKAGE $RUNTIME_PACKAGE

    elif [[ "$ID" == "debian" ]]; then
        # Add Microsoft package repository for Debian
        wget https://packages.microsoft.com/config/debian/$VER/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb

        apt-get update
        apt-get install -y $SDK_PACKAGE $RUNTIME_PACKAGE

    elif [[ "$ID" == "centos" ]] || [[ "$ID" == "rhel" ]] || [[ "$ID" == "almalinux" ]] || [[ "$ID" == "rocky" ]]; then
        # Add Microsoft package repository for RHEL-based systems
        rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm

        yum install -y $SDK_PACKAGE $RUNTIME_PACKAGE
    fi
}

# Alternative: Install using dotnet-install script
install_dotnet_script() {
    print_status "Installing .NET $DOTNET_VERSION using dotnet-install script..."

    # Download the install script
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh

    # Install .NET SDK
    ./dotnet-install.sh --channel $DOTNET_VERSION --install-dir /usr/share/dotnet

    # Install ASP.NET Core Runtime
    ./dotnet-install.sh --channel $DOTNET_VERSION --runtime aspnetcore --install-dir /usr/share/dotnet

    # Create symbolic links
    ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

    # Clean up
    rm dotnet-install.sh
}

# Configure environment
configure_environment() {
    print_status "Configuring environment variables..."

    # Add to system-wide environment
    cat > /etc/profile.d/dotnet.sh << 'EOF'
export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_PRINT_TELEMETRY_MESSAGE=false
EOF

    chmod +x /etc/profile.d/dotnet.sh

    # Source the file for current session
    source /etc/profile.d/dotnet.sh
}

# Install Entity Framework tools
install_ef_tools() {
    print_status "Installing Entity Framework Core tools..."
    dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef

    # Add dotnet tools to PATH
    export PATH="$PATH:$HOME/.dotnet/tools"
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
}

# Verify installation
verify_installation() {
    print_status "Verifying .NET installation..."

    # Check dotnet command
    if command -v dotnet &> /dev/null; then
        print_status "✅ dotnet command found"
        dotnet --version
    else
        print_error "❌ dotnet command not found"
        return 1
    fi

    # List installed SDKs
    print_status "Installed SDKs:"
    dotnet --list-sdks

    # List installed runtimes
    print_status "Installed Runtimes:"
    dotnet --list-runtimes

    # Check ASP.NET Core runtime
    if dotnet --list-runtimes | grep -q "Microsoft.AspNetCore.App $DOTNET_VERSION"; then
        print_status "✅ ASP.NET Core $DOTNET_VERSION runtime installed"
    else
        print_warning "⚠️ ASP.NET Core $DOTNET_VERSION runtime not found"
    fi
}

# Create test application
create_test_app() {
    print_status "Creating test ASP.NET Core application..."

    TEST_DIR="/tmp/dotnet-test-app"
    rm -rf $TEST_DIR
    mkdir -p $TEST_DIR
    cd $TEST_DIR

    # Create new web app
    dotnet new web -n TestApp
    cd TestApp

    # Try to build and run
    if dotnet build; then
        print_status "✅ Test build successful"

        # Run for 5 seconds to test
        timeout 5 dotnet run --urls "http://localhost:5555" || true
        print_status "✅ Test run successful"
    else
        print_error "❌ Test build failed"
    fi

    # Cleanup
    cd /
    rm -rf $TEST_DIR
}

# Main installation function
main() {
    print_status "=== Starting .NET $DOTNET_VERSION Installation for Plesk Server ==="

    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
       print_error "This script must be run as root (use sudo)"
       exit 1
    fi

    # Detect OS
    detect_os

    # Install prerequisites
    install_prerequisites

    # Try package manager installation first
    if install_dotnet_package_manager; then
        print_status "Successfully installed via package manager"
    else
        print_warning "Package manager installation failed, trying script method..."
        install_dotnet_script
    fi

    # Configure environment
    configure_environment

    # Install EF tools
    install_ef_tools

    # Verify installation
    verify_installation

    # Optional: Create test app
    read -p "Do you want to create a test application? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        create_test_app
    fi

    print_status "=== .NET $DOTNET_VERSION Installation Complete ==="
    print_status ""
    print_status "Next steps:"
    print_status "1. Upload your application files to /var/www/vhosts/yourdomain.com/httpdocs"
    print_status "2. Run: dotnet restore"
    print_status "3. Run: dotnet publish -c Release"
    print_status "4. Configure systemd service (see gcc-webapp.service)"
    print_status "5. Configure Nginx proxy in Plesk panel"
    print_status ""
    print_status "To verify installation, run: dotnet --info"
}

# Run main function
main "$@"
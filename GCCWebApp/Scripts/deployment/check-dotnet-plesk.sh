#!/bin/bash
# Check .NET installations on Plesk

echo "=== Checking .NET installations ==="

# Check standard locations
echo -e "\n1. Standard dotnet command:"
which dotnet 2>/dev/null || echo "dotnet not found in PATH"

# Check Plesk-specific locations
echo -e "\n2. Checking Plesk .NET locations:"
ls -la /opt/plesk/dotnet* 2>/dev/null || echo "No Plesk dotnet directory found"
ls -la /usr/local/psa/var/modules/dotnet-toolkit* 2>/dev/null || echo "No toolkit directory found"

# Check system packages
echo -e "\n3. Installed .NET packages:"
rpm -qa | grep -i dotnet 2>/dev/null || dpkg -l | grep -i dotnet 2>/dev/null || echo "No packages found"

# Check environment
echo -e "\n4. Environment variables:"
env | grep -i dotnet

# Try to find dotnet installations
echo -e "\n5. Searching for dotnet installations:"
find /opt /usr -name "dotnet" -type f 2>/dev/null | head -10

echo -e "\n6. Checking Plesk extension status:"
plesk ext --list 2>/dev/null | grep -i "net" || echo "Cannot access Plesk CLI"
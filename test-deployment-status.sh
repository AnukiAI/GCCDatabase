#!/bin/bash
# Quick deployment status check script
echo "=== GCC Deployment Status Check ==="

echo "1. Checking Docker..."
docker --version

echo "2. Checking MariaDB..."
systemctl status mysql | head -5

echo "3. Testing database connection..."
mysql -h localhost -P 3306 -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk gccdb -e "SELECT 1;" 2>/dev/null && echo "✅ Database OK" || echo "❌ Database connection failed"

echo "4. Checking if container exists..."
docker ps -a | grep gcc-webapp

echo "5. Checking port 5102..."
netstat -tulpn | grep 5102

echo "6. Testing GitHub registry access..."
docker pull ghcr.io/anukiai/gcc-webapp:latest --quiet && echo "✅ Registry access OK" || echo "❌ Registry access failed"

echo "=== Status Check Complete ==="
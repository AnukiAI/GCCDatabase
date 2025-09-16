#!/bin/bash
# Docker Container Diagnostics Script
# Run this on the Plesk server to diagnose why gcc-webapp keeps stopping

CONTAINER_NAME="gcc-webapp"
echo "🔍 GCC Docker Container Diagnostics"
echo "====================================="

# 1. Check if Docker is running
echo "1. Docker Status:"
if command -v docker &> /dev/null; then
    echo "✅ Docker is installed"
    docker --version
    if docker info &> /dev/null; then
        echo "✅ Docker daemon is running"
    else
        echo "❌ Docker daemon is not running"
        exit 1
    fi
else
    echo "❌ Docker is not installed"
    exit 1
fi

echo ""

# 2. Check container status
echo "2. Container Status:"
if docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -q "$CONTAINER_NAME"; then
    echo "Container found:"
    docker ps -a --filter name=$CONTAINER_NAME --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}\t{{.Image}}"

    # Check if running
    if docker ps --filter name=$CONTAINER_NAME --format "{{.Names}}" | grep -q "$CONTAINER_NAME"; then
        echo "✅ Container is RUNNING"
    else
        echo "❌ Container is STOPPED"

        # Get exit code
        EXIT_CODE=$(docker inspect $CONTAINER_NAME --format='{{.State.ExitCode}}' 2>/dev/null)
        echo "Exit Code: $EXIT_CODE"

        # Explain exit codes
        case $EXIT_CODE in
            0) echo "→ Container stopped normally" ;;
            1) echo "→ General application error" ;;
            125) echo "→ Docker daemon error" ;;
            126) echo "→ Container command not executable" ;;
            127) echo "→ Container command not found" ;;
            139) echo "→ Segmentation fault - Architecture mismatch or missing dependencies" ;;
            143) echo "→ Container received SIGTERM (graceful shutdown)" ;;
            *) echo "→ Unknown exit code - check logs" ;;
        esac
    fi
else
    echo "❌ Container not found"
fi

echo ""

# 3. Show recent logs
echo "3. Container Logs (Last 20 lines):"
echo "--------------------------------"
docker logs $CONTAINER_NAME --tail 20 2>&1 || echo "No logs available"

echo ""

# 4. Check port conflicts
echo "4. Port Usage:"
echo "Port 80:" $(netstat -tln 2>/dev/null | grep :80 || echo "Available")
echo "Port 443:" $(netstat -tln 2>/dev/null | grep :443 || echo "Available")
echo "Port 5101:" $(netstat -tln 2>/dev/null | grep :5101 || echo "Available")
echo "Port 8080:" $(netstat -tln 2>/dev/null | grep :8080 || echo "Available")

echo ""

# 5. System resources
echo "5. System Resources:"
echo "Memory:"
free -h | head -2
echo "Disk space:"
df -h /var/lib/docker 2>/dev/null || df -h /

echo ""

# 6. Docker events
echo "6. Recent Docker Events:"
echo "------------------------"
docker events --since '10m ago' --until now --filter container=$CONTAINER_NAME 2>/dev/null | tail -10 || echo "No recent events"

echo ""

# 7. Quick fixes
echo "7. Troubleshooting Steps:"
echo "========================"

echo "A. Check what's using port 80:"
echo "   sudo netstat -tlnp | grep :80"
echo ""

echo "B. Restart container:"
echo "   docker restart $CONTAINER_NAME"
echo ""

echo "C. View live logs:"
echo "   docker logs -f $CONTAINER_NAME"
echo ""

echo "D. Run in debug mode:"
echo "   docker run -it --rm -p 8080:8080 \\"
echo "     -e ASPNETCORE_ENVIRONMENT=Development \\"
echo "     -e ConnectionStrings__DefaultConnection=\"Data Source=/tmp/gcc_database.db\" \\"
echo "     ghcr.io/anukiai/gcc-webapp:production-ready"
echo ""

echo "E. Remove and redeploy:"
echo "   docker stop $CONTAINER_NAME && docker rm $CONTAINER_NAME"
echo "   # Then run deployment script again"
echo ""

# 8. Common issues detection
echo "8. Issue Detection:"
echo "==================="

# Check memory
AVAILABLE_MEM=$(free -m | awk 'NR==2{printf "%.0f", $7}')
if [ "$AVAILABLE_MEM" -lt 500 ]; then
    echo "⚠️  LOW MEMORY: Only ${AVAILABLE_MEM}MB available. Container may be killed by OOM."
fi

# Check if Apache/Nginx is using port 80
if netstat -tln 2>/dev/null | grep :80 | grep -q "apache\|nginx"; then
    echo "⚠️  PORT CONFLICT: Web server is using port 80. Use different port or configure proxy."
fi

# Check image architecture
IMAGE_ARCH=$(docker inspect ghcr.io/anukiai/gcc-webapp:production-ready --format='{{.Architecture}}' 2>/dev/null)
SYSTEM_ARCH=$(uname -m)
if [ "$IMAGE_ARCH" != "" ] && [ "$SYSTEM_ARCH" != "" ]; then
    if [[ "$IMAGE_ARCH" != *"amd64"* ]] && [[ "$SYSTEM_ARCH" == *"x86_64"* ]]; then
        echo "⚠️  ARCHITECTURE MISMATCH: Image is $IMAGE_ARCH but system is $SYSTEM_ARCH"
    fi
fi

echo ""
echo "Diagnostics complete. Review the output above for issues."
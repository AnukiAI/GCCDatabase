#!/bin/bash

echo "=== Fixing Plesk Deployment Issue ==="

# Step 1: Build image with explicit AMD64 architecture
echo "Building AMD64-specific image..."
docker build --platform linux/amd64 -t ghcr.io/anukiai/gcc-webapp:amd64 -f Dockerfile.plesk .

# Step 2: Test locally
echo "Testing image locally..."
docker run -d --name gcc-test-amd64 \
  -p 5103:8080 \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  ghcr.io/anukiai/gcc-webapp:amd64

sleep 5
curl -I http://localhost:5103 && echo "Local test successful!" || echo "Local test failed"

# Step 3: Push to registry
echo "Pushing to GitHub Container Registry..."
docker push ghcr.io/anukiai/gcc-webapp:amd64

# Step 4: Tag as latest
docker tag ghcr.io/anukiai/gcc-webapp:amd64 ghcr.io/anukiai/gcc-webapp:latest
docker push ghcr.io/anukiai/gcc-webapp:latest

# Cleanup
docker stop gcc-test-amd64
docker rm gcc-test-amd64

echo "=== Fix Complete ==="
echo ""
echo "Now on Plesk server, run:"
echo "docker pull ghcr.io/anukiai/gcc-webapp:latest"
echo "docker run -d --name gcc-webapp --restart unless-stopped \\"
echo "  -p 5101:8080 \\"
echo "  -e ConnectionStrings__DefaultConnection='Data Source=/app/data/gcc_database.db' \\"
echo "  -v /var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/data:/app/data \\"
echo "  ghcr.io/anukiai/gcc-webapp:latest"
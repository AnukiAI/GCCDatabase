#!/bin/bash

# Build multi-architecture Docker image for Plesk deployment
# Supports both AMD64 and ARM64 architectures

echo "=== Building Multi-Architecture GCC Docker Image ==="

# Enable Docker buildx
docker buildx create --name multiarch-builder --use || docker buildx use multiarch-builder
docker buildx inspect --bootstrap

# Build and push multi-architecture image
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t ghcr.io/anukiai/gcc-webapp:latest \
  -t ghcr.io/anukiai/gcc-webapp:multiarch \
  -f Dockerfile \
  --push \
  .

echo "Multi-architecture build complete!"
echo "Image pushed to: ghcr.io/anukiai/gcc-webapp:latest"
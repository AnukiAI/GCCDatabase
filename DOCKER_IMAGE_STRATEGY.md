# Docker Image Versioning and Tagging Strategy

## Overview

This document outlines the Docker image versioning and tagging strategy for the GCC Web Application, ensuring consistent, traceable, and manageable container deployments across all environments.

---

## Tagging Strategy

### Environment-Based Tags

Each environment has specific tag patterns to ensure proper image segregation:

| Environment | Primary Tag | Additional Tags | Example |
|------------|-------------|-----------------|---------|
| Development | `dev-latest` | `dev-{build}`, `dev-{date}` | `gcc-webapp:dev-latest`, `gcc-webapp:dev-20240830_1234` |
| Test | `test-latest` | `test-{build}`, `test-{version}` | `gcc-webapp:test-latest`, `gcc-webapp:test-v1.2.3` |
| Staging | `staging-latest` | `staging-{build}`, `staging-rc-{version}` | `gcc-webapp:staging-latest`, `gcc-webapp:staging-rc-1.2.3` |
| Production | `latest`, `stable` | `prod-{version}`, `v{semver}` | `gcc-webapp:latest`, `gcc-webapp:v1.2.3` |

### Semantic Versioning Tags

Production releases follow semantic versioning (MAJOR.MINOR.PATCH):

```
gcc-webapp:v1.0.0    # Major release
gcc-webapp:v1.1.0    # Minor release (new features)
gcc-webapp:v1.1.1    # Patch release (bug fixes)
gcc-webapp:v2.0.0    # Breaking changes
```

### Build-Specific Tags

Every build gets a unique identifier:

```
gcc-webapp:{env}-{date}_{time}
gcc-webapp:{env}-{git-commit}
gcc-webapp:{env}-build-{number}
```

Examples:
- `gcc-webapp:dev-20240830_143022`
- `gcc-webapp:test-abc123f`
- `gcc-webapp:prod-build-456`

---

## Versioning Workflow

### 1. Development Phase
```bash
# Feature branch builds
gcc-webapp:feature-{branch-name}
gcc-webapp:dev-{commit-sha}

# Daily development builds
gcc-webapp:dev-latest
gcc-webapp:dev-nightly-{date}
```

### 2. Testing Phase
```bash
# Test environment deployments
gcc-webapp:test-latest
gcc-webapp:test-{build-number}
gcc-webapp:test-snapshot-{date}
```

### 3. Release Candidates
```bash
# Pre-production releases
gcc-webapp:rc-1.2.3
gcc-webapp:staging-rc-1.2.3
gcc-webapp:beta-1.2.3
```

### 4. Production Release
```bash
# Production deployments
gcc-webapp:v1.2.3
gcc-webapp:1.2.3
gcc-webapp:latest
gcc-webapp:stable
```

---

## Registry Organization

### Docker Hub Structure
```
dockerhub.com/
└── your-org/
    └── gcc-webapp/
        ├── Tags:
        │   ├── latest (production)
        │   ├── stable (last stable)
        │   ├── v1.2.3 (versions)
        │   ├── dev-latest
        │   ├── test-latest
        │   └── staging-latest
        └── Automated Builds:
            └── From GitHub/GitLab
```

### Private Registry Structure
```
registry.company.com/
└── gcc/
    ├── webapp/
    │   ├── production/
    │   ├── staging/
    │   ├── test/
    │   └── development/
    └── database/
        └── migrations/
```

---

## Tagging Commands

### Local Development
```bash
# Build with multiple tags
docker build -t gcc-webapp:dev-latest \
             -t gcc-webapp:dev-$(date +%Y%m%d) \
             -t gcc-webapp:dev-$(git rev-parse --short HEAD) \
             .
```

### CI/CD Pipeline
```bash
# Automated tagging based on branch
if [ "$BRANCH" = "main" ]; then
    TAG="latest"
elif [ "$BRANCH" = "develop" ]; then
    TAG="test-latest"
else
    TAG="dev-${BRANCH//\//-}"
fi

docker build -t gcc-webapp:$TAG .
docker push gcc-webapp:$TAG
```

### Release Process
```bash
# Tag for release
VERSION="1.2.3"
docker tag gcc-webapp:staging-latest gcc-webapp:v$VERSION
docker tag gcc-webapp:v$VERSION gcc-webapp:latest
docker push gcc-webapp:v$VERSION
docker push gcc-webapp:latest
```

---

## Image Lifecycle Management

### Retention Policies

| Environment | Retention Period | Policy |
|------------|-----------------|--------|
| Development | 7 days | Keep last 10 images |
| Test | 30 days | Keep last 20 images |
| Staging | 90 days | Keep all RC versions |
| Production | 1 year | Keep all tagged versions |

### Cleanup Strategy

```bash
# Remove old development images
docker image prune -a --filter "label=environment=dev" --filter "until=168h"

# Remove untagged images
docker image prune -f

# Remove specific version ranges
docker images | grep "dev-" | awk '{print $3}' | xargs docker rmi
```

### Automated Cleanup (Registry)

Docker Hub:
```yaml
# Docker Hub automated cleanup
retention_policy:
  dev:
    max_images: 10
    max_age_days: 7
  test:
    max_images: 20
    max_age_days: 30
  prod:
    keep_all_tagged: true
```

AWS ECR:
```json
{
  "rules": [{
    "rulePriority": 1,
    "description": "Keep only 10 development images",
    "selection": {
      "tagStatus": "tagged",
      "tagPrefixList": ["dev-"],
      "countType": "imageCountMoreThan",
      "countNumber": 10
    },
    "action": { "type": "expire" }
  }]
}
```

---

## Promotion Strategy

### Environment Promotion Path

```
Development → Test → Staging → Production
    dev-*      test-*   staging-*   v*.*.* 
```

### Promotion Commands

```bash
# Promote from dev to test
docker tag gcc-webapp:dev-latest gcc-webapp:test-candidate
docker push gcc-webapp:test-candidate

# Promote from test to staging
docker tag gcc-webapp:test-approved gcc-webapp:staging-latest
docker push gcc-webapp:staging-latest

# Promote from staging to production
docker tag gcc-webapp:staging-approved gcc-webapp:v1.2.3
docker tag gcc-webapp:v1.2.3 gcc-webapp:latest
docker push gcc-webapp:v1.2.3
docker push gcc-webapp:latest
```

### Automated Promotion (CI/CD)

```yaml
# GitHub Actions example
- name: Promote Image
  if: github.event_name == 'release'
  run: |
    SOURCE_TAG="staging-latest"
    TARGET_TAG="v${{ github.event.release.tag_name }}"
    
    docker pull gcc-webapp:$SOURCE_TAG
    docker tag gcc-webapp:$SOURCE_TAG gcc-webapp:$TARGET_TAG
    docker tag gcc-webapp:$TARGET_TAG gcc-webapp:latest
    docker push gcc-webapp:$TARGET_TAG
    docker push gcc-webapp:latest
```

---

## Multi-Architecture Strategy

### Supported Architectures

- `linux/amd64` - Standard x86_64 servers
- `linux/arm64` - ARM servers, Apple Silicon
- `windows/amd64` - Windows containers (optional)

### Building Multi-Arch Images

```bash
# Setup buildx
docker buildx create --name multiarch --use

# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --tag gcc-webapp:latest \
  --push .

# Inspect manifest
docker manifest inspect gcc-webapp:latest
```

---

## Security Considerations

### Image Signing

```bash
# Sign images with Docker Content Trust
export DOCKER_CONTENT_TRUST=1
docker push gcc-webapp:v1.2.3

# Verify signature
docker trust inspect gcc-webapp:v1.2.3
```

### Vulnerability Scanning

```bash
# Scan before pushing
docker scan gcc-webapp:latest
trivy image gcc-webapp:latest

# Block vulnerable images
if [ $(trivy image --severity HIGH,CRITICAL --exit-code 1 gcc-webapp:latest) ]; then
    echo "Image has vulnerabilities, blocking push"
    exit 1
fi
```

### Secret Management

Never include secrets in images:
- Use build arguments for build-time secrets
- Use environment variables for runtime secrets
- Use Docker secrets or Kubernetes secrets in production

---

## Metadata and Labels

### Standard Labels

```dockerfile
LABEL version="1.2.3"
LABEL description="GCC Web Application"
LABEL maintainer="devops@greencirclecertified.com"
LABEL org.opencontainers.image.created="2024-08-30"
LABEL org.opencontainers.image.authors="GCC DevOps"
LABEL org.opencontainers.image.url="https://github.com/gcc/webapp"
LABEL org.opencontainers.image.version="1.2.3"
LABEL org.opencontainers.image.revision="${GIT_COMMIT}"
LABEL org.opencontainers.image.licenses="MIT"
LABEL environment="${ENVIRONMENT}"
LABEL build.number="${BUILD_NUMBER}"
LABEL build.date="${BUILD_DATE}"
```

### Querying by Labels

```bash
# Find all production images
docker images --filter "label=environment=production"

# Find images built on specific date
docker images --filter "label=build.date=2024-08-30"

# Find specific version
docker images --filter "label=version=1.2.3"
```

---

## Best Practices

### Do's ✅

1. **Always tag with multiple identifiers**
   - Environment tag
   - Version tag
   - Build number/date

2. **Use immutable tags for production**
   - Never overwrite version tags
   - Use semantic versioning

3. **Implement automated cleanup**
   - Set retention policies
   - Remove unused images regularly

4. **Sign and scan production images**
   - Enable Docker Content Trust
   - Run vulnerability scans

5. **Document tag meanings**
   - Maintain clear naming conventions
   - Document in README

### Don'ts ❌

1. **Never use only 'latest' tag**
   - Always include version tags
   - 'latest' should be an alias

2. **Don't overwrite production tags**
   - Version tags are immutable
   - Use new versions for changes

3. **Avoid generic names**
   - Be specific with naming
   - Include environment context

4. **Don't mix environments**
   - Keep clear separation
   - Use different registries if needed

5. **Never include secrets**
   - No passwords in images
   - No API keys in layers

---

## Quick Reference

### Common Commands

```bash
# Build with tags
docker build -t gcc-webapp:dev-latest .

# Push to registry
docker push gcc-webapp:dev-latest

# Tag existing image
docker tag gcc-webapp:dev-latest gcc-webapp:test-latest

# List images with pattern
docker images "gcc-webapp:*"

# Remove old images
docker image prune -a --filter "until=24h"

# Inspect image labels
docker inspect gcc-webapp:latest | jq '.[0].Config.Labels'

# Pull specific version
docker pull gcc-webapp:v1.2.3
```

### Environment Variables

```bash
# Build arguments
BUILD_NUMBER=123
GIT_COMMIT=$(git rev-parse HEAD)
BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
VERSION=$(git describe --tags --always)

# Registry variables
REGISTRY_URL=docker.io
REGISTRY_USER=your-username
IMAGE_NAME=gcc-webapp
```

---

## Migration Guide

### From Existing Tags

If migrating from an existing tagging scheme:

1. **Audit current tags**
   ```bash
   docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.CreatedAt}}"
   ```

2. **Map to new scheme**
   ```bash
   # Old: webapp:latest → New: gcc-webapp:prod-latest
   docker tag webapp:latest gcc-webapp:prod-latest
   ```

3. **Update CI/CD pipelines**
   - Update build scripts
   - Update deployment manifests
   - Update documentation

4. **Gradual migration**
   - Keep old tags temporarily
   - Update services gradually
   - Remove old tags after verification

---

**Document Version**: 1.0  
**Last Updated**: August 2024  
**Owner**: GCC DevOps Team

---
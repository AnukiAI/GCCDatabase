# GCC Database Solution - AI Assistant Guide

This file provides guidance to Claude Code (claude.ai/code) when working with this codebase.

## Quick Start Commands

```bash
# Build and Run
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5101"

# Database
dotnet ef database update
dotnet ef migrations add [MigrationName]

# Testing
dotnet test                                    # All tests
dotnet test GCCWebApp.Tests                   # Unit tests
dotnet test GCCWebApp.UAT.Tests              # UI tests
```

## Project Structure

```
/Controllers        # MVC controllers and API endpoints
/Services          # Business logic and rules
/Repositories      # Data access layer
/Models            # Domain models and ViewModels
/Views             # Razor views (UI templates)
/Data              # EF Core context and migrations
/wwwroot           # Static files (CSS, JS, images)
```

## Key Technical Details

- **Framework**: ASP.NET Core 8.0
- **Database**: SQLite (Dev) / SQL Server (Prod)
- **Authentication**: Azure AD B2C + Cookie Auth fallback
- **PDF Generation**: QuestPDF library
- **Testing**: xUnit, Playwright, SpecFlow

## Critical Requirements to Remember

### 1. Certificate Tile View (Landing Page)
- **NO PAGINATION** - Must use lazy loading/infinite scroll
- Autocomplete search with suggestions dropdown
- Unique URLs for filtered searches (for direct linking)
- Sort and filter capabilities on all public columns
- Download certificate directly from tile

### 2. Search Functionality
- Case-insensitive partial matching
- Display results in dropdown below search bar
- Show "no results" message when appropriate
- Must support autocomplete suggestions

### 3. Partner Support
- Three partners: Amazon, Wayfair, None
- Partner field already in database (migration: 20250827182202)
- Partner-specific filtering required
- Partner-specific branding (not yet implemented)

### 4. Certificate Workflow
- 14-state lifecycle already implemented
- Status transitions enforced by CertificateWorkflowService
- Role-based permissions for status changes

## Current Implementation Gaps

### High Priority (Sprint Focus)
1. **Autocomplete Search** - Not implemented
2. **Lazy Loading** - Currently using pagination instead
3. **Unique URLs** - Search state not preserved in URL
4. **Excel Export** - Only basic CSV, needs formatting
5. **Audit Trail UI** - Backend exists, no UI

### Medium Priority
- Geographic filtering
- Saved search preferences
- Partner-specific branding
- Bulk operations UI

## Important File Locations

- **Main Certificate Logic**: `/Controllers/CertificateController.cs`
- **Business Rules**: `/Services/CertificateService.cs`
- **Workflow Service**: `/Services/CertificateWorkflowService.cs`
- **Data Access**: `/Repositories/CertificateRepository.cs`
- **Certificate Views**: `/Views/Certificate/`
- **Mockup Designs**: `/0_Documentation/pages/`

## Documentation References

### Core Requirements Documents
- **Full Requirements & User Stories**: `0_Documentation/docs/Requirements.md`
  - US-001 to US-010: Complete user stories with acceptance criteria
  - TC-001 to TC-012: Detailed test cases
  - Progress tracking and implementation status
- **Mockup Designs**: `0_Documentation/pages/`
  - Certificate views, dashboard, admin pages
  - Reference these for UI implementation

### Analysis & Gap Documents
- **Mockup vs Implementation Gap**: `0_Documentation/docs/Mockup-vs-Implementation-Gap-Analysis-2025-08-27.md`
- **Requirements vs Implementation**: `0_Documentation/docs/Requirements-vs-Implementation-Analysis-2025-08-27.md`
- **Test Coverage Analysis**: `0_Documentation/docs/Test-Coverage-Analysis-Report.md`
- **Application Issues List**: `0_Documentation/docs/Application-Issues-List.md`

### UAT & Testing Documents
- **Manual UAT Test Cases**: `0_Documentation/docs/MANUAL_UAT_TEST_CASES.md`
- **UAT Execution Roadmap**: `0_Documentation/docs/UAT_EXECUTION_ROADMAP.md`
- **UAT Test Environment Guide**: `0_Documentation/docs/UAT_TEST_ENVIRONMENT_GUIDE.md`
- **Test Credentials Inventory**: `0_Documentation/docs/Test-Credentials-Inventory.md`
- **API Testing Strategy**: `0_Documentation/docs/API-Testing-Strategy-and-Implementation.md`

### Implementation Summaries
- **Critical Security Fixes**: `0_Documentation/docs/Critical-Security-Fixes-Summary.md`
- **High Priority Fixes**: `0_Documentation/docs/High-Priority-Fixes-Summary.md`
- **Test Coverage Improvements**: `0_Documentation/docs/Test-Coverage-Improvements-Summary.md`
- **Mockup Integration**: `0_Documentation/docs/Mockup-Integration-Implementation-Summary.md`
- **Development Session Summary**: `0_Documentation/docs/Development-Session-Summary-2025-08-27.md`

### User Stories Quick Reference
- **US-001**: Certificate Creation (Admin/Analyst)
- **US-002**: Certificate Search & Filter (Public)
- **US-003**: Certificate Tile View (Public)
- **US-004**: Certificate Lifecycle Management (Admin/Analyst)
- **US-005**: Partner-Specific Certificates (External)
- **US-006**: Analytics Dashboard (Admin/Analyst)
- **US-007**: Export Functionality (All Users)
- **US-008**: User Authentication (All Users)
- **US-009**: Role Management (Admin)
- **US-010**: RESTful API (Third-party Systems)

## Application Sitemap / Routes

### Public Pages (No Authentication Required)
```
/                                    # Home page (redirects to /Certificate/CertificateTileView)
/Certificate/CertificateTileView     # Main landing page - Certificate tile view
/Certificate/Details/{id}            # Certificate detail view (public)
/Certificate/Download/{id}           # Download certificate PDF
/Account/Login                       # Login page
/Account/Register                    # Registration page
/Account/ForgotPassword             # Password reset
```

### Authenticated Pages

#### Certificate Management (Admin/Analyst)
```
/Certificate/Certificates            # Certificate list (table view)
/Certificate/Create                  # Create new certificate
/Certificate/Edit/{id}              # Edit certificate
/Certificate/Delete/{id}            # Delete certificate
/Certificate/Export                 # Export certificates (CSV/PDF)
/Certificate/CertificateSingleViewInternal  # Internal certificate view
```

#### Dashboard & Reporting (Admin/Analyst)
```
/Dashboard                          # Analytics dashboard
/Dashboard/Export                   # Export dashboard data
/Dashboard/Metrics                  # Detailed metrics view
```

#### Admin Pages (Admin Only)
```
/Admin/UserManagement               # User management
/Admin/RoleManagement              # Role assignments
/Admin/Partners                    # Partner management
/Admin/Companies                   # Company management
/Admin/Products                    # Product management
/Admin/Settings                    # System settings
/Admin/AuditLog                    # Audit trail viewer
```

#### Account Management (All Authenticated Users)
```
/Account/Manage                    # User profile
/Account/ChangePassword           # Change password
/Account/TwoFactorAuth            # 2FA settings
/Account/Logout                   # Logout
```

### API Endpoints

#### Public API
```
GET    /api/v1/certificates        # List certificates (paginated)
GET    /api/v1/certificates/{id}   # Get certificate details
GET    /api/v1/products            # List products
GET    /api/v1/companies          # List companies
```

#### Authenticated API (Requires API Key or JWT)
```
POST   /api/v1/certificates        # Create certificate
PUT    /api/v1/certificates/{id}   # Update certificate
DELETE /api/v1/certificates/{id}   # Delete certificate
GET    /api/v1/certificates/export # Export certificates
POST   /api/v1/apikeys             # Create API key
DELETE /api/v1/apikeys/{id}        # Revoke API key
```

#### ETL & Integration (Admin Only)
```
POST   /api/etl/import             # Import certificates
POST   /api/etl/export             # Export all data
GET    /api/etl/status             # ETL job status
```

### Route Access Matrix
```
Route Pattern                | Public | External | Internal | Analyst | Admin
----------------------------|--------|----------|----------|---------|-------
/Certificate/TileView       |   ✓    |    ✓     |    ✓     |    ✓    |   ✓
/Certificate/Create         |   ✗    |    ✗     |    ✗     |    ✓    |   ✓
/Dashboard                  |   ✗    |    ✗     |    ✓     |    ✓    |   ✓
/Admin/*                    |   ✗    |    ✗     |    ✗     |    ✗    |   ✓
/api/v1/* (GET)            |   ✓    |    ✓     |    ✓     |    ✓    |   ✓
/api/v1/* (POST/PUT/DELETE)|   ✗    |    ✗     |    ✗     |    ✓    |   ✓
```

## Test Credentials

```
Admin: admin@greencirclecertified.com / Admin123!
Analyst: analyst@greencirclecertified.com / Analyst123!
External: partner@amazon.com / Partner123!
```

## Database Information

### Connection Strings
```json
// Development (SQLite)
"DefaultConnection": "Data Source=gcc_database.db"

// Production (SQL Server)
"DefaultConnection": "Server=server;Database=GCCDatabase;User Id=user;Password=pass;"
```

### Key Tables
```
Certificates        # Main certificate records
Companies          # Company information
Products           # Product catalog
Users              # User accounts
ApiKeys            # API key management
AuditLogs          # Audit trail records
CertificateStatus  # Status history tracking
```

### Recent Migrations
- `20250827182202_AddPartnerFieldToCertificate` - Added Partner support
- `20250827043046_AddPerformanceIndexes` - Performance optimization

## Environment Variables & Configuration

### Required Settings (Production)
```bash
# Core Application
ASPNETCORE_ENVIRONMENT=Production
GCC_JWT_SECRET=64-character-secure-random-string

# Azure AD B2C (Optional - Cookie auth fallback available)
GCC_AZURE_AD_B2C_CLIENT_SECRET=azure-b2c-client-secret  
GCC_AZURE_AD_CLIENT_SECRET=azure-ad-client-secret

# Database
DATABASE_CONNECTION_STRING="Server=prod-server;Database=GCCDatabase;User Id=appuser;Password=secure-password;Encrypt=true;"

# Optional Services
GCC_AZURE_BLOB_CONNECTION_STRING=azure-storage-connection
GCC_APPLICATION_INSIGHTS_CONNECTION_STRING=app-insights-string
GCC_SMTP_PASSWORD=email-server-password

# JWT Configuration
JWT__Issuer=https://api.greencirclecertified.com
JWT__Audience=https://api.greencirclecertified.com
```

### Feature Flags
```json
"Features": {
  "EnableAzureB2C": true,
  "EnableTwoFactor": true,
  "EnableApiRateLimiting": true,
  "EnableLazyLoading": false,  // TODO: Implement
  "EnableAutocomplete": false   // TODO: Implement
}
```

## Common Debugging Commands

```bash
# Check running application
curl http://localhost:5101/health

# View application logs
tail -f Logs/app-*.log

# Reset database (Dev only)
dotnet ef database drop
dotnet ef database update

# Generate mock data
cd Scripts && RunMockDataGeneration.cmd

# Run specific test category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=Performance

# Check code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Performance Considerations

### Current Bottlenecks
1. **Certificate List Loading**: Using pagination instead of lazy loading
2. **Search Performance**: No caching, direct DB queries
3. **Export Operations**: Large datasets timeout

### Optimization Guidelines
- Use `IQueryable` for deferred execution
- Implement caching for frequently accessed data
- Use async/await throughout
- Add database indexes for search columns
- Consider implementing Redis for session state

## API Documentation

### API Authentication Methods

#### 1. API Key Authentication (Recommended for Partners)
```http
GET /api/CertificateTileExternal/tiles
X-API-Key: your-api-key-here
```

#### 2. JWT Bearer Token
```http
POST /api/v1/auth/login
Content-Type: application/json
{
  "username": "user@example.com",
  "password": "password"
}

# Response
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expires": "2025-08-29T12:00:00Z"
}

# Use token in subsequent requests
GET /api/Certificates
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### API Response Formats

#### Success Response
```json
{
  "success": true,
  "data": {
    "certificates": [...],
    "totalCount": 150
  },
  "metadata": {
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

#### Error Response
```json
{
  "success": false,
  "errors": [
    {
      "code": "CERT_NOT_FOUND",
      "message": "Certificate with ID 123 not found",
      "field": "certificateId"
    }
  ],
  "timestamp": "2025-08-28T10:30:00Z"
}
```

### API Rate Limiting
- **Default**: 100 requests per minute per API key
- **Authenticated**: 500 requests per minute
- **Headers returned**:
  - `X-RateLimit-Limit`: Maximum requests allowed
  - `X-RateLimit-Remaining`: Requests remaining
  - `X-RateLimit-Reset`: Unix timestamp when limit resets

### OpenAPI/Swagger
- **Development**: http://localhost:5101/swagger
- **Production**: https://api.greencirclecertified.com/swagger
- **Download spec**: `/swagger/v1/swagger.json`

### Partner API Endpoints (Amazon Integration Ready)

#### Certificate Access for External Partners
```http
GET    /api/CertificateTileExternal/tiles          # Get partner's certificates
GET    /api/CertificateTileExternal/suggestions    # Search suggestions  
GET    /api/CertificateTileExternal/filters        # Available filters
GET    /api/CertificateTileExternal/certificate/{id} # Get specific certificate
POST   /api/CertificateTileExternal/export         # Export partner's data
```

#### Amazon-Specific Features
- **Automatic Partner Filtering**: Only shows Amazon-tagged certificates
- **Rate Limiting**: 1000 API calls/hour (configurable per partner)
- **IP Restrictions**: Amazon server IPs only
- **Origin Validation**: *.amazon.com domains only
- **Test Account**: partner@amazon.com / Partner123! (External role)

## Docker Deployment

### Current Container Status (as of September 15, 2025)

```bash
# Check running containers
docker ps

# Current containers:
# - gcc-production: Running on port 5105 (healthy) - gcc-webapp:production
# - gcc-webapp-fixed: Running on port 5102 (unhealthy) - gcc-webapp:production-fixed
```

### Available Deployment Scripts

The project includes comprehensive Docker deployment automation:

#### 1. Interactive Build Script - `docker-build.cmd`
Full-featured build menu with options for:
- Development, Test, Production builds
- Multi-architecture builds (ARM64, AMD64)
- Security scanning
- Registry deployment
- Build history and logging

```bash
# Windows
./docker-build.cmd

# Options available:
# 1. Build Development Image
# 2. Build Test Image
# 3. Build Production Image
# 4. Build All Environments
# 5. Build and Push to Registry
# 6. Build Multi-Architecture Image
# 7. Security Scan Image
# 8. Clean Build (No Cache)
```

#### 2. Quick Production Deployment - `docker-run-production.sh`
```bash
# Run production container with SQLite
./docker-run-production.sh
# Starts container on port 5105
```

#### 3. Plesk Server Deployment - `docker-deploy-plesk-FIXED.cmd`
```bash
# Deploy to Plesk server with PostgreSQL
./docker-deploy-plesk-FIXED.cmd
# Includes proper environment configuration
```

#### 4. Registry-Based Deployment
```bash
# Deploy from container registry (GitHub Packages)
./docker-deploy-plesk-registry-UPDATED.cmd
./docker-deploy-plesk-registry-MARIADB.cmd
```

#### 5. Docker Compose Deployment
```bash
# Full stack with PostgreSQL + Redis
docker-compose up -d

# With MariaDB
docker-compose -f docker-compose.mariadb.yml up -d
```

### Current Dockerfile Configuration
```dockerfile
# Production-optimized Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy and restore dependencies
COPY ["GCCWebApp.csproj", "."]
RUN dotnet restore "./GCCWebApp.csproj"

# Build application
COPY . .
RUN dotnet build "./GCCWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "./GCCWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app

# Security: Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GCCWebApp.dll"]
```

### Docker Management Scripts

#### Utility Scripts Available
```bash
# Build and deployment
./docker-build.cmd              # Interactive build menu (comprehensive)
./docker-build.sh               # Linux/bash version
./docker-run-production.sh      # Quick production container start

# Plesk server deployment
./docker-deploy-plesk-FIXED.cmd       # Fixed Plesk deployment with PostgreSQL
./docker-deploy-plesk-FIXED.sh        # Linux version
./docker-deploy-plesk-simple.sh       # Simplified Plesk deployment
./docker-deploy-plesk-registry.cmd    # Deploy from container registry

# Container management
./docker-cleanup.cmd            # Clean up old containers and images
./docker-health-check.cmd       # Health check script
./docker-rollback.cmd           # Rollback to previous version
./docker-push.cmd               # Push to registry

# Multi-architecture builds
./docker-buildx-multiarch.sh    # Build for multiple platforms (ARM64, AMD64)

# Testing
./docker-deploy-test.cmd        # Test deployment
```

### Docker Commands

#### Current Production Containers
```bash
# Check status of running containers
docker ps | grep gcc

# Current containers (September 2025):
# gcc-production:     port 5105 (healthy)   - SQLite database
# gcc-webapp-fixed:   port 5102 (unhealthy) - needs attention

# Check container health
docker inspect gcc-production --format='{{.State.Health.Status}}'

# View container logs
docker logs -f gcc-production
docker logs -f gcc-webapp-fixed
```

#### Build and Run Commands
```bash
# Build production image
docker build -t gcc-webapp:production .

# Run with SQLite (current working config)
docker run -d \
  --name gcc-webapp \
  -p 5105:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  gcc-webapp:production

# Run with PostgreSQL (Plesk configuration)
docker run -d \
  --name gcc-webapp \
  -p 5102:5101 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Host=localhost;Database=gccdb;Username=gccuser;Password=YourPassword;Port=5432" \
  -e Jwt__Key="ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789" \
  gcc-webapp:production
```

#### Docker Compose (Current Configuration)
```yaml
# docker-compose.yml - PostgreSQL + Redis Stack
services:
  gccwebapp:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: gcc-webapp
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=gcc_database;Username=gcc_user;Password=${DB_PASSWORD:-defaultpassword123}
      - Jwt__Key=${JWT_SECRET_KEY:-your-super-secret-jwt-key-min-64-chars-for-security-compliance-here}
      - DatabaseProvider=postgresql
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped
    networks:
      - gcc-network

  postgres:
    image: postgres:15-alpine
    container_name: gcc-postgres
    environment:
      - POSTGRES_DB=gcc_database
      - POSTGRES_USER=gcc_user
      - POSTGRES_PASSWORD=${DB_PASSWORD:-defaultpassword123}
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U gcc_user -d gcc_database"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - gcc-network

  redis:
    image: redis:7-alpine
    container_name: gcc-redis
    ports:
      - "6379:6379"
    command: redis-server --requirepass ${REDIS_PASSWORD:-redispassword123}
    volumes:
      - redis-data:/data
    networks:
      - gcc-network

volumes:
  postgres-data:
  redis-data:
  app-logs:

networks:
  gcc-network:
    driver: bridge
```

Available compose configurations:
```bash
# PostgreSQL stack (recommended)
docker-compose up -d

# MariaDB stack
docker-compose -f docker-compose.mariadb.yml up -d

# Test environment
docker-compose -f docker-compose.test.yml up -d
```

#### Docker Compose Commands
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f webapp

# Stop all services
docker-compose down

# Rebuild and restart
docker-compose up -d --build

# Scale webapp service
docker-compose up -d --scale webapp=3
```

### Container Registry Deployment

#### Azure Container Registry
```bash
# Login to ACR
az acr login --name gcccontainerregistry

# Tag image
docker tag gcc-webapp:latest gcccontainerregistry.azurecr.io/gcc-webapp:latest

# Push to registry
docker push gcccontainerregistry.azurecr.io/gcc-webapp:latest

# Deploy to Azure App Service
az webapp create \
  --resource-group gcc-rg \
  --plan gcc-app-plan \
  --name gcc-webapp \
  --deployment-container-image-name gcccontainerregistry.azurecr.io/gcc-webapp:latest
```

#### Docker Hub
```bash
# Login to Docker Hub
docker login

# Tag for Docker Hub
docker tag gcc-webapp:latest yourusername/gcc-webapp:latest

# Push to Docker Hub
docker push yourusername/gcc-webapp:latest
```

### Container Troubleshooting & Next Steps

#### Current Issues to Address (September 2025)
1. **gcc-webapp-fixed container (port 5102)**: Status is unhealthy
   ```bash
   # Check specific health status
   docker inspect gcc-webapp-fixed --format='{{.State.Health.Status}}'

   # View detailed logs
   docker logs --tail 50 gcc-webapp-fixed

   # Restart if needed
   docker restart gcc-webapp-fixed
   ```

2. **Database Configuration**: Multiple database options available
   - SQLite: Working in gcc-production container
   - PostgreSQL: Configured for Plesk deployment
   - MariaDB: Alternative option via compose

#### Recommended Next Steps
```bash
# 1. Fix unhealthy container
docker stop gcc-webapp-fixed
docker rm gcc-webapp-fixed

# 2. Deploy fresh container using working configuration
./docker-deploy-plesk-FIXED.cmd

# 3. Or use Docker Compose for full stack
docker-compose up -d

# 4. Monitor health status
watch docker ps
```

#### Container Health Monitoring

##### Health Check Endpoint
```csharp
// Already implemented in Program.cs
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

##### Docker Health Check (Built-in)
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

##### Container Monitoring Commands
```bash
# View container logs
docker logs gcc-production
docker logs gcc-webapp-fixed

# Follow logs in real-time
docker logs -f gcc-production

# View last 100 lines
docker logs --tail 100 gcc-production

# Check container stats
docker stats gcc-production

# Inspect container configuration
docker inspect gcc-production | jq '.Config.Env'
```

##### Environment Variables Verification
```bash
# Check current container environment
docker exec gcc-production env | grep -E "(ASPNETCORE|ConnectionStrings|Jwt)"

# Should show:
# ASPNETCORE_ENVIRONMENT=Production
# ASPNETCORE_URLS=http://+:8080
# ConnectionStrings__DefaultConnection=Data Source=/app/data/gcc_database.db
```

### Docker Deployment Quick Reference

#### Production Deployment Options (Choose One)
1. **SQLite + Single Container** (Currently Working)
   ```bash
   ./docker-run-production.sh
   # Starts on port 5105, uses SQLite database
   ```

2. **PostgreSQL + Docker Compose** (Recommended for Scalability)
   ```bash
   docker-compose up -d
   # Full stack: App + PostgreSQL + Redis
   # App runs on port 8080
   ```

3. **Plesk Server Deployment** (For Hosting Provider)
   ```bash
   ./docker-deploy-plesk-FIXED.cmd
   # Configured for Plesk hosting with PostgreSQL
   ```

4. **Interactive Build & Deploy**
   ```bash
   ./docker-build.cmd
   # Choose option 3 for production build
   # Then select deployment method
   ```

#### Container Status Summary
```
Container Name      | Port | Status    | Database | Purpose
-------------------|------|-----------|----------|------------------
gcc-production     | 5105 | Healthy   | SQLite   | Working production
gcc-webapp-fixed   | 5102 | Unhealthy | Postgres | Needs attention
```

#### Emergency Recovery
```bash
# If all containers fail, rebuild from scratch:
docker stop $(docker ps -q --filter "name=gcc")
docker rm $(docker ps -aq --filter "name=gcc")
./docker-build.cmd  # Choose option 3 (Production)
./docker-run-production.sh
```

### Kubernetes Deployment (Optional)

#### Deployment YAML
```yaml
# k8s-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gcc-webapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: gcc-webapp
  template:
    metadata:
      labels:
        app: gcc-webapp
    spec:
      containers:
      - name: gcc-webapp
        image: gcccontainerregistry.azurecr.io/gcc-webapp:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: gcc-secrets
              key: connection-string
```

#### Deploy to Kubernetes
```bash
# Create namespace
kubectl create namespace gcc

# Create secrets
kubectl create secret generic gcc-secrets \
  --from-literal=connection-string="Server=..." \
  -n gcc

# Apply deployment
kubectl apply -f k8s-deployment.yaml -n gcc

# Expose service
kubectl expose deployment gcc-webapp \
  --type=LoadBalancer \
  --port=80 \
  --target-port=80 \
  -n gcc

# Check status
kubectl get pods -n gcc
kubectl get services -n gcc
```

## Security Checklist

### Before Each Release
- [ ] Run security tests: `dotnet test GCCWebApp.Security.Tests`
- [ ] Check for hardcoded credentials
- [ ] Verify input validation on all forms
- [ ] Test rate limiting on APIs
- [ ] Review authentication flows
- [ ] Check CORS configuration
- [ ] Validate JWT token expiration
- [ ] Test role-based access control
- [ ] Scan Docker image for vulnerabilities: `docker scan gcc-webapp:latest`
- [ ] Review container security policies

## Development Notes

### When Implementing Features
1. Check mockups in `/0_Documentation/pages/` first
2. Follow existing patterns in the codebase
3. Update tests when adding new functionality
4. Use repository pattern for data access
5. Implement service layer for business logic
6. Add logging for important operations
7. Update CLAUDE.md for significant changes

### Common Issues & Solutions
- **PDF Generation**: Already implemented with QuestPDF
- **Partner Field**: Already in database, use existing migration
- **Status Workflow**: Use CertificateWorkflowService
- **Authentication**: Azure AD B2C configured, falls back to cookies
- **Date Handling**: Use UTC in database, local time in UI
- **File Uploads**: Check FileUploadService for existing implementation

### Git Workflow
```bash
git checkout -b feature/your-feature
# Make changes
git add .
git commit -m "feat: description"
# Create PR - don't push directly to master
```

### Commit Message Format
```
feat: Add new feature
fix: Fix bug
docs: Update documentation
style: Format code
refactor: Refactor code
test: Add tests
chore: Update dependencies
```

## Code Modification Best Practices

### Safe Code Modification Strategies

#### 1. Read First, Understand Context
- Always use `Read` tool before editing any file
- Check surrounding code for dependencies and interactions
- Understand the full flow and impact before changing anything
- Review related files that might be affected

#### 2. Minimal, Targeted Edits
- Use specific `old_string` matches with enough context to ensure uniqueness
- Make the smallest possible changes to achieve the goal
- Prefer `Edit` over `Write` for existing files
- Use `MultiEdit` for multiple changes to the same file

#### 3. Test-Driven Approach
- Run existing tests before making changes
- Verify functionality still works after each edit
- Create test cases for new features first
- Run build and lint commands after changes

#### 4. Version Control Awareness
- Check git status frequently
- Make atomic commits for each logical change
- Review changes before committing
- Can revert if something breaks

#### 5. Defensive Coding Patterns
- Add new functionality without disturbing existing code
- Use feature flags for new features when appropriate
- Preserve existing method signatures when possible
- Add new methods rather than modifying existing ones when feasible

## AI Assistant Instructions

### DO:
- Follow lazy loading requirement (no pagination) for certificates
- Implement autocomplete for search functionality
- Create unique URLs for filtered searches
- Use existing services and patterns
- Check mockups before implementing UI
- Read files before editing them
- Use minimal, targeted edits
- Test changes thoroughly

### DON'T:
- Use pagination for certificate views
- Create new authentication systems (use existing)
- Modify the 14-state workflow without discussion
- Push directly to master branch
- Create files without checking if they exist
- Replace entire methods when only small changes are needed
- Make changes without understanding context

### ALWAYS:
- Run tests after changes
- Follow repository pattern
- Use ViewModels for UI binding
- Check `/0_Documentation/pages/` for UI requirements
- Update this file when major changes are made
- Read before editing
- Preserve working code
- Use version control effectively

## 🏪 Amazon API Integration - Production Ready

### Amazon Integration Status: ✅ READY FOR TESTING & PRODUCTION

The GCC Database Solution API is **fully implemented** and **production-ready** for Amazon integration with the following features:

#### **Amazon-Specific Implementation**
- **Dedicated External API Controller**: `CertificateTileExternalApiController.cs`
- **Automatic Partner Filtering**: Only shows Amazon-tagged certificates + public certificates
- **Test Account Ready**: `partner@amazon.com` / `Partner123!` (External role)
- **API Key Authentication**: Secure, rate-limited access for Amazon systems

#### **Available Endpoints for Amazon**
```bash
# Core Certificate Access
GET    /api/CertificateTileExternal/tiles          # Get Amazon's certificates
GET    /api/CertificateTileExternal/suggestions    # Search autocomplete
GET    /api/CertificateTileExternal/filters        # Available filter options
GET    /api/CertificateTileExternal/certificate/{id} # Get specific certificate details
POST   /api/CertificateTileExternal/export         # Export Amazon's data (CSV/Excel/PDF)

# Authentication & API Key Management
POST   /api/v1/auth/login                          # JWT token authentication
POST   /Admin/CreateApiKey                         # Create Amazon API key (Admin only)
```

#### **Security & Performance Features**
- **Rate Limiting**: 1000 API calls/hour (configurable per partner)
- **IP Whitelisting**: Restrict to Amazon server IPs
- **Origin Validation**: Only *.amazon.com domains
- **Request Logging**: Full audit trail of all API calls
- **Automatic Failover**: Cookie auth backup if API key fails

#### **Testing Amazon Integration Right Now**
```bash
# 1. Test public certificate access (no auth required)
curl -X GET "http://localhost:5101/api/CertificateTileExternal/tiles" \
     -H "accept: application/json"

# 2. Login as Amazon test user
curl -X POST "http://localhost:5101/Account/Login" \
     -d "email=partner@amazon.com&password=Partner123!"

# 3. Create API key for Amazon (after login)
curl -X POST "http://localhost:5101/Admin/CreateApiKey" \
     -H "Content-Type: application/json" \
     -d '{"name": "Amazon Integration", "description": "Amazon marketplace API access"}'

# 4. Test with API key
curl -X GET "http://localhost:5101/api/CertificateTileExternal/tiles?partner=Amazon" \
     -H "X-API-Key: generated-api-key-here"
```

### **Production Deployment Checklist**

#### ✅ **Ready Components**
- [x] **Core API Implementation**: All endpoints working
- [x] **Authentication Systems**: API Key + JWT + Cookie fallback
- [x] **Partner Filtering**: Amazon data isolation implemented  
- [x] **Rate Limiting**: Configurable per-partner limits
- [x] **Security Headers**: Production-grade security middleware
- [x] **Error Handling**: Standardized API responses
- [x] **Documentation**: Swagger/OpenAPI fully configured
- [x] **Docker Support**: Container deployment ready

#### 🔧 **Needs Production Configuration**
- [ ] **JWT Secret**: Set `GCC_JWT_SECRET` environment variable (64+ characters)
- [ ] **Database**: Configure production SQL Server connection string
- [ ] **SSL Certificate**: HTTPS setup for production domain
- [ ] **Azure Services** (Optional): B2C, Application Insights, Blob Storage
- [ ] **SMTP Configuration** (Optional): Email notifications

#### **Estimated Time to Production**: 2-4 hours (configuration only)

### **Amazon Developer Quick Start**

1. **Get API Access**:
   - Contact GCC admin for Amazon-specific API key
   - Receive API key with Amazon domain/IP restrictions

2. **Test Integration**:
   - Use development endpoint: `http://localhost:5101/api/CertificateTileExternal/`
   - Test with Amazon test account: `partner@amazon.com`

3. **Production Integration**:
   - Switch to production endpoint: `https://api.greencirclecertified.com/api/CertificateTileExternal/`
   - Use production API key with IP/domain restrictions

4. **Handle API Responses**:
   ```json
   {
     "success": true,
     "data": [...certificates...],
     "message": null,
     "requestId": "unique-request-id",
     "timestamp": "2025-09-08T15:30:00Z"
   }
   ```

**Amazon integration is FULLY FUNCTIONAL and ready for immediate testing and production deployment.**

---
*Last Updated: September 8, 2025*
- remember to suggest
- remember the system is using .NET MVC and SQL Server, please update documentaiton to reflect this. I see Django and postgre in docs
- We need to maintain the design as we are making updates. We should not be adding additional functionality
- during coding, test that the javascript is rendering properly in the page. this has been causing a lot of issues.
- There should not be a search bar in the navigation
- minimum font size in the application is 16px for accessibility
- When new use case functionality is added to the project. Update test projects to account for that scenario
- Master Design Guide should be referenced to make sure our design is conistent throughout the application. C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\0_Documentation\mockups\0_0_MasterDesignGuide.html
- Reference Wireframes and User Stories to make sure we are implementing what was agreed upon with the client. C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\0_Documentation\00 GCC_ImplementationDocuments_8_22_25\GCC Wireframes_UserStories_8_25_25.pptx
- we are going to complete development on all of the pages.
- remember Admin role and user cannot be deleted as per requirements
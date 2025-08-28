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

### Required Settings
```
ASPNETCORE_ENVIRONMENT=Development|Production
AzureAdB2C__Instance=https://gccb2c.b2clogin.com/
AzureAdB2C__Domain=gccb2c.onmicrosoft.com
AzureAdB2C__ClientId=[Your Client ID]
AzureAdB2C__ClientSecret=[Your Secret]
JWT__SecretKey=[Your JWT Secret]
JWT__Issuer=https://greencirclecertified.com
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

#### 1. API Key Authentication
```http
GET /api/v1/certificates
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
GET /api/v1/certificates
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

## Docker Deployment

### Dockerfile
```dockerfile
# Already exists in project root
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GCCWebApp.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GCCWebApp.dll"]
```

### Docker Commands

#### Build and Run Locally
```bash
# Build Docker image
docker build -t gcc-webapp:latest .

# Run container (Development)
docker run -d \
  --name gcc-webapp \
  -p 5101:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/gcc_database.db" \
  -v ${PWD}/data:/data \
  gcc-webapp:latest

# Run container (Production)
docker run -d \
  --name gcc-webapp \
  -p 80:80 \
  -p 443:443 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=sql-server;Database=GCC;User Id=sa;Password=YourPassword" \
  -e AzureAdB2C__ClientSecret="your-secret" \
  -v ${PWD}/certs:/https \
  gcc-webapp:latest
```

#### Docker Compose
```yaml
# docker-compose.yml (exists in project)
version: '3.8'

services:
  webapp:
    build: .
    ports:
      - "5101:80"
      - "5102:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=GCC;User Id=sa;Password=YourPassword
    depends_on:
      - db
    volumes:
      - ./data:/data
      - ./logs:/app/logs

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql

volumes:
  sql-data:
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

### Health Checks & Monitoring

#### Health Check Endpoint
```csharp
// Already implemented in Program.cs
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

#### Docker Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost/health || exit 1
```

#### Container Logs
```bash
# View container logs
docker logs gcc-webapp

# Follow logs
docker logs -f gcc-webapp

# View last 100 lines
docker logs --tail 100 gcc-webapp
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

## AI Assistant Instructions

### DO:
- Follow lazy loading requirement (no pagination) for certificates
- Implement autocomplete for search functionality
- Create unique URLs for filtered searches
- Use existing services and patterns
- Check mockups before implementing UI

### DON'T:
- Use pagination for certificate views
- Create new authentication systems (use existing)
- Modify the 14-state workflow without discussion
- Push directly to master branch
- Create files without checking if they exist

### ALWAYS:
- Run tests after changes
- Follow repository pattern
- Use ViewModels for UI binding
- Check `/0_Documentation/pages/` for UI requirements
- Update this file when major changes are made

---
*Last Updated: August 28, 2025*
- remember to suggest
- remember the system is using .NET MVC and SQL Server, please update documentaiton to reflect this. I see Django and postgre in docs
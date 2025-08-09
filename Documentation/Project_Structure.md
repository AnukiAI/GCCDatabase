# GCC Database Application - Project Structure

## Solution Overview

The GCC Database Application solution is organized into multiple projects following clean architecture principles:

```
GCC Database Solution/
├── GCCWebApp/
│   ├── GCCWebApp/                 # Main web application
│   ├── GCCWebApp.Tests/           # Unit tests
│   ├── GCCWebApp.UAT.Tests/       # User acceptance tests
│   └── GCCWebApp.E2E.Tests/       # End-to-end tests with UI automation
```

## Main Application (GCCWebApp)

```
GCCWebApp/
├── Controllers/
│   ├── AccountController.cs       # Authentication and user management
│   ├── AdminController.cs         # Administrative functions
│   ├── CertificateController.cs   # Certificate management
│   ├── DashboardController.cs     # Dashboard and analytics
│   └── Api/                       # REST API controllers
│       ├── CertificatesApiController.cs
│       ├── CompaniesApiController.cs
│       └── UsersApiController.cs
│
├── Models/
│   ├── Certificate.cs             # Certificate entity
│   ├── Company.cs                 # Company entity
│   ├── Product.cs                 # Product entity
│   ├── User.cs                    # User entity
│   ├── Role.cs                    # Role entity
│   ├── Tag.cs                     # Tag entity
│   ├── CustomField.cs             # Custom field entity
│   ├── CustomFieldValue.cs        # Custom field values
│   ├── CertificationType.cs       # Certificate types
│   ├── ProductCategory.cs         # Product categories
│   ├── ProductSubcategory.cs      # Product subcategories
│   ├── ProductType.cs             # Product types
│   ├── AdminSettings.cs           # Admin configuration
│   └── ViewModels/
│       ├── CertificateViewModel.cs
│       ├── CertificateListViewModel.cs
│       ├── CertificateEditViewModel.cs
│       ├── CertificateListViewModelSimple.cs
│       └── Dashboard.cs
│
├── Services/
│   ├── IServices.cs               # Service interfaces
│   ├── CertificateService.cs      # Certificate business logic
│   ├── AdminService.cs            # Admin operations
│   ├── CacheService.cs            # Caching layer
│   ├── FilterService.cs           # Search and filtering
│   ├── FileUploadService.cs       # File handling
│   └── ValidationService.cs       # Business rule validation
│
├── Repositories/
│   ├── IRepository.cs             # Repository interface
│   ├── Repository.cs              # Generic repository
│   ├── ICertificateRepository.cs  # Certificate repository interface
│   ├── CertificateRepository.cs   # Certificate repository
│   ├── ICompanyRepository.cs      # Company repository interface
│   ├── CompanyRepository.cs       # Company repository
│   ├── IUserRepository.cs         # User repository interface
│   └── UserRepository.cs          # User repository
│
├── Data/
│   ├── ApplicationDbContext.cs    # Entity Framework context
│   ├── DbInitializer.cs          # Database seeding
│   └── Migrations/                # EF Core migrations
│       ├── 20240808_InitialCreate.cs
│       ├── 20240809_AddAdminSystem.cs
│       └── [Additional migrations]
│
├── Configuration/
│   └── AzureAdB2COptions.cs      # Azure AD B2C settings
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml        # Main layout
│   │   ├── _LoginPartial.cshtml  # Login/logout partial
│   │   └── Error.cshtml           # Error page
│   ├── Account/
│   │   ├── Login.cshtml          # Login page
│   │   ├── SignUp.cshtml         # Registration page
│   │   ├── ForgotPassword.cshtml # Password recovery
│   │   ├── ResetPassword.cshtml  # Password reset
│   │   ├── 2FAAuthenticate.cshtml # Two-factor auth
│   │   ├── AccountLocked.cshtml  # Account lockout
│   │   ├── VerifyEmail.cshtml    # Email verification
│   │   └── WelcomeRedirect.cshtml # Post-registration
│   ├── Certificate/
│   │   ├── Index.cshtml          # Certificate list
│   │   ├── Details.cshtml        # Certificate details
│   │   ├── Create.cshtml         # Create certificate
│   │   └── Edit.cshtml           # Edit certificate
│   ├── Dashboard/
│   │   └── Index.cshtml          # Dashboard view
│   └── Admin/
│       ├── Index.cshtml          # Admin dashboard
│       ├── Users.cshtml          # User management
│       ├── Roles.cshtml          # Role management
│       └── Settings.cshtml       # System settings
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css             # Main styles
│   │   └── admin.css            # Admin styles
│   ├── js/
│   │   ├── site.js              # Main JavaScript
│   │   └── admin.js             # Admin JavaScript
│   ├── lib/                     # Third-party libraries
│   └── images/                  # Static images
│
├── Documentation/
│   ├── README.md                 # Main documentation
│   ├── API_Reference.md          # API documentation
│   ├── Architecture_Overview.md  # System architecture
│   ├── Database_Design.md        # Database schema
│   ├── Developer_Setup_Guide.md  # Setup instructions
│   ├── Testing_Guide.md          # Testing documentation
│   ├── Deployment_Guide.md       # Deployment instructions
│   ├── Security_Guide.md         # Security best practices
│   ├── User_Guide.md             # End-user documentation
│   ├── Admin_System_Guide.md     # Admin documentation
│   ├── Azure_AD_B2C_Setup_Guide.md # Azure AD setup
│   ├── Monitoring_Guide.md       # Monitoring setup
│   ├── Troubleshooting_Guide.md  # Common issues
│   └── Project_Structure.md      # This file
│
├── Program.cs                    # Application entry point
├── appsettings.json             # Configuration
├── appsettings.Development.json  # Dev configuration
└── gcc_database.db              # SQLite database file

```

## Test Projects

### GCCWebApp.Tests (Unit Tests)
```
GCCWebApp.Tests/
├── Controllers/
│   ├── CertificateControllerTests.cs
│   ├── AdminControllerTests.cs
│   └── AccountControllerTests.cs
├── Services/
│   ├── CertificateServiceTests.cs
│   ├── AdminServiceTests.cs
│   └── ValidationServiceTests.cs
├── Repositories/
│   ├── CertificateRepositoryTests.cs
│   └── CompanyRepositoryTests.cs
├── Helpers/
│   └── TestDataBuilder.cs
└── GCCWebApp.Tests.csproj
```

### GCCWebApp.UAT.Tests (User Acceptance Tests)
```
GCCWebApp.UAT.Tests/
├── Scenarios/
│   ├── CertificateManagementTests.cs
│   ├── UserWorkflowTests.cs
│   └── AdminScenarioTests.cs
├── Fixtures/
│   └── TestDataFixture.cs
└── GCCWebApp.UAT.Tests.csproj
```

### GCCWebApp.E2E.Tests (End-to-End Tests)
```
GCCWebApp.E2E.Tests/
├── Fixtures/
│   └── TestWebApplicationFactory.cs  # Test server setup
├── Tests/
│   ├── CertificateApiTests.cs       # API integration tests
│   ├── AuthenticationE2ETests.cs    # Auth flow tests
│   └── UI/
│       └── CertificateUITests.cs    # Playwright UI tests
├── README.md                         # E2E test documentation
└── GCCWebApp.E2E.Tests.csproj
```

## Key Design Patterns

### Repository Pattern
- **Purpose**: Abstracts data access logic
- **Location**: `/Repositories`
- **Usage**: All database operations go through repositories

### Service Layer Pattern
- **Purpose**: Contains business logic
- **Location**: `/Services`
- **Usage**: Controllers use services, services use repositories

### MVC Pattern
- **Purpose**: Separates presentation, logic, and data
- **Components**: 
  - Models: Data and business entities
  - Views: User interface
  - Controllers: Request handling and orchestration

### Dependency Injection
- **Configuration**: `Program.cs`
- **Usage**: All services and repositories are injected

### Unit of Work Pattern
- **Implementation**: Through Entity Framework's DbContext
- **Purpose**: Manages database transactions

## Configuration Files

### Application Settings
- `appsettings.json` - Production configuration
- `appsettings.Development.json` - Development overrides
- Environment variables for sensitive data

### Connection Strings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gcc_database.db"
  }
}
```

### Azure AD B2C Configuration
```json
{
  "AzureAdB2C": {
    "Instance": "https://[tenant].b2clogin.com",
    "ClientId": "[client-id]",
    "Domain": "[tenant].onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn"
  }
}
```

## Database Structure

### Main Entities
- **Certificates**: Core certification records
- **Companies**: Organizations holding certificates
- **Products**: Products covered by certificates
- **Users**: System users
- **Roles**: User roles and permissions

### Relationships
- One-to-Many: Company → Certificates
- One-to-Many: Product → Certificates
- Many-to-Many: Users ↔ Roles
- Many-to-Many: Certificates ↔ Tags

## Testing Strategy

### Test Pyramid
1. **Unit Tests** (40%) - Fast, isolated component tests
2. **Integration Tests** (30%) - Component interaction tests
3. **UAT Tests** (20%) - Business scenario validation
4. **E2E Tests** (10%) - Full workflow with UI

### Test Execution
```bash
# Run all tests
dotnet test

# Run specific project
dotnet test GCCWebApp.Tests
dotnet test GCCWebApp.UAT.Tests
dotnet test GCCWebApp.E2E.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Build and Deployment

### Local Development
```bash
cd GCCWebApp
dotnet restore
dotnet ef database update
dotnet run
```

### Production Build
```bash
dotnet publish -c Release -o ./publish
```

### Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "GCCWebApp.dll"]
```

## Monitoring and Logging

### Logging Providers
- Console (Development)
- Application Insights (Production)
- File logging (Optional)

### Health Checks
- Database connectivity
- External service availability
- Application readiness

## Security Considerations

### Authentication
- Azure AD B2C for external users
- Cookie authentication fallback
- Two-factor authentication support

### Authorization
- Role-based access control (RBAC)
- Policy-based authorization
- Resource-based permissions

### Data Protection
- BCrypt password hashing
- HTTPS enforcement
- SQL injection prevention via EF Core
- XSS protection via Razor encoding

## Performance Optimization

### Caching Strategy
- In-memory caching for frequently accessed data
- Redis cache for distributed scenarios
- Response caching for static content

### Database Optimization
- Indexed columns for search
- Eager loading for related data
- Pagination for large datasets

## Future Enhancements

### Planned Features
- GraphQL API support
- Real-time notifications (SignalR)
- Advanced reporting module
- Mobile application
- Microservices architecture

### Technical Debt
- Migrate to .NET 9.0
- Implement CQRS pattern
- Add event sourcing
- Enhance test coverage to 80%+
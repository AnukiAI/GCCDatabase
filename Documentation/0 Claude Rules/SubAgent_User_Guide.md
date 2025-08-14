# Sub-Agent User Guide for GCC Web Application

## Overview
This guide explains how to effectively use the specialized Claude sub-agents for your GreenCircle Certified (GCC) Web Application project. Each sub-agent is designed with deep knowledge of your specific ASP.NET Core architecture and business domain.

## How to Invoke Sub-Agents

### Basic Invocation Pattern
```
"I need the [SUB-AGENT NAME] to help with [specific task]"
```

### Examples:
- "I need the DATABASE_ARCHITECT to help optimize the Certificate search queries"
- "I need the SECURITY_SPECIALIST to review the authentication flow"
- "I need the API_ARCHITECT to design a new endpoint for product filtering"

## Sub-Agent Reference Guide

### 1. DATABASE_ARCHITECT
**When to Use:**
- Database schema changes or optimizations
- Entity Framework migrations
- Query performance issues
- Data relationship problems
- Database seeding or initialization issues

**Sample Requests:**
- "I need to add a new field to the Certificate entity"
- "The product search is slow - help optimize the database queries"
- "Create a migration to add indexing for better search performance"
- "I'm getting foreign key constraint errors"

**Expertise Areas:**
- `gcc_database.db` SQLite management
- Entity relationships (Certificate, Product, Company hierarchies)
- Migration management (20+ existing migrations)
- `SeedData.InitializeAsync()` configuration
- Custom field and tag schema design

---

### 2. SECURITY_SPECIALIST
**When to Use:**
- Authentication or authorization issues
- Security configuration problems
- Role-based access implementation
- API security concerns
- Security headers and policies

**Sample Requests:**
- "Users can't log in through Azure AD B2C"
- "I need to add a new role to the system"
- "The API endpoints need better security"
- "Help implement proper CORS policies"
- "Review security headers configuration"

**Expertise Areas:**
- Azure AD B2C with `AzureAdB2COptions`
- JWT and API key authentication
- Role system: Admin, Analyst, External, Public
- Security middleware pipeline
- Authorization policies and claims

---

### 3. API_ARCHITECT
**When to Use:**
- REST API design and implementation
- API versioning strategies
- Swagger/OpenAPI documentation
- API performance optimization
- External API integration

**Sample Requests:**
- "Design a new API endpoint for bulk certificate updates"
- "The API responses are too slow"
- "Help implement proper API versioning"
- "Create Swagger documentation for the new endpoints"
- "I need to add pagination to the certificate API"

**Expertise Areas:**
- `Controllers/Api/v1/` structure
- `BaseApiController` pattern
- API versioning with Asp.Versioning
- Swagger configuration and documentation
- Rate limiting and CORS

---

### 4. FRONTEND_SPECIALIST
**When to Use:**
- MVC view development
- User interface improvements
- Client-side functionality
- Responsive design issues
- Form validation and user experience

**Sample Requests:**
- "Create a new admin interface for managing tags"
- "The certificate upload form needs better validation"
- "Make the product management interface more user-friendly"
- "Add export functionality to the admin dashboard"
- "Improve the mobile responsiveness"

**Expertise Areas:**
- Views structure: `Admin/`, `Certificate/`, `Product/`
- Custom CSS: `product-management.css`, `tags.css`
- JavaScript utilities: `admin/exportUtils.js`
- Bootstrap integration and responsive design
- Role-based UI rendering

---

### 5. BUSINESS_LOGIC_SPECIALIST
**When to Use:**
- Business rule implementation
- Service layer architecture
- Workflow management
- Data validation logic
- Process automation

**Sample Requests:**
- "Implement certificate approval workflow"
- "Add business rules for product categorization"
- "Create validation logic for custom fields"
- "Help design the certificate lifecycle states"
- "Implement company-product relationship rules"

**Expertise Areas:**
- Service interfaces: `ICertificateService`, `IFilterService`
- Business rule enforcement through `ValidationService`
- Certificate lifecycle management
- Product hierarchy validation
- Repository pattern with `IUnitOfWork`

---

### 6. ETL_SPECIALIST
**When to Use:**
- Data import/export operations
- ETL pipeline development
- Background service implementation
- Data transformation logic
- External data source integration

**Sample Requests:**
- "Set up automated certificate data imports"
- "Create ETL pipeline for product catalog sync"
- "The background ETL service isn't working"
- "Add data validation to the import process"
- "Monitor ETL performance and errors"

**Expertise Areas:**
- ETL services: `IETLService`, `IDataExtractor`, `IDataTransformer`
- `ETLSchedulerService` for background processing
- ETL API endpoints: `/api/v1/etl/status`
- Data pipeline orchestration
- External API integration for data sources

---

### 7. TESTING_SPECIALIST
**When to Use:**
- Test strategy development
- Test implementation and debugging
- Test coverage analysis
- Performance testing
- Security testing

**Sample Requests:**
- "Create unit tests for the new certificate service"
- "Set up E2E tests for the authentication flow"
- "The integration tests are failing"
- "Add performance tests for the API endpoints"
- "Create security tests for authorization"

**Expertise Areas:**
- Test projects: `GCCWebApp.Tests`, `GCCWebApp.E2E.Tests`, `GCCWebApp.UAT.Tests`
- xUnit framework with `TestWebApplicationFactory`
- Playwright for browser automation
- Page Object Model: `LoginPage`, `CertificatePage`
- Role-based workflow testing

---

### 8. DEVOPS_SPECIALIST
**When to Use:**
- Deployment configuration
- Environment setup
- Performance optimization
- Configuration management
- Infrastructure concerns

**Sample Requests:**
- "Configure the application for production deployment"
- "Set up environment-specific configurations"
- "The application is slow in production"
- "Help with database connection issues"
- "Configure logging and monitoring"

**Expertise Areas:**
- `appsettings.json` configuration management
- `Program.cs` middleware pipeline
- Security headers and HSTS configuration
- Static file caching and optimization
- Database initialization and connection management

---

### 9. INTEGRATION_SPECIALIST
**When to Use:**
- Third-party API integration
- External service connections
- Document management systems
- Data synchronization
- Webhook implementations

**Sample Requests:**
- "Integrate with Amazon's product catalog API"
- "Set up Google Analytics tracking"
- "Create document upload functionality"
- "Connect to external certificate authorities"
- "Implement real-time data synchronization"

**Expertise Areas:**
- `HttpClientFactory` configuration
- Amazon API integration (`AmazonAPI.cshtml`)
- Google Analytics integration (`googleAnalytics.js`)
- `FileUploadService` for document management
- External API authentication and retry policies

---

### 10. PERFORMANCE_SPECIALIST
**When to Use:**
- Application performance issues
- Database query optimization
- Memory management problems
- Caching strategy implementation
- Scalability concerns

**Sample Requests:**
- "The certificate search is too slow"
- "Optimize memory usage for large datasets"
- "Implement caching for frequently accessed data"
- "The API responses have high latency"
- "Monitor and improve ETL performance"

**Expertise Areas:**
- Memory and distributed caching strategies
- `CacheService` implementation
- Database query optimization with EF Core
- Rate limiting with `AspNetCoreRateLimit`
- Static file optimization and compression

## Best Practices for Sub-Agent Usage

### 1. Be Specific About Context
Instead of: "Help with authentication"
Use: "I need the SECURITY_SPECIALIST to help configure Azure AD B2C for our production environment"

### 2. Provide Relevant Details
- Include error messages or specific symptoms
- Mention which part of the application is affected
- Provide relevant code snippets if applicable

### 3. Use Multiple Sub-Agents for Complex Tasks
Example: "I need the DATABASE_ARCHITECT to design the schema and the API_ARCHITECT to create the endpoints for a new feature"

### 4. Leverage Sub-Agent Collaboration
Sub-agents understand each other's domains and can reference related work:
- "Work with the SECURITY_SPECIALIST to ensure the new API endpoints are properly secured"
- "Coordinate with the TESTING_SPECIALIST to create tests for this implementation"

### 5. Ask for Implementation Guidance
- "Show me the exact code changes needed"
- "Provide step-by-step implementation instructions"
- "What are the potential risks and how do I mitigate them?"

## Common Multi-Agent Scenarios

### New Feature Development
1. **DATABASE_ARCHITECT**: Design schema changes
2. **BUSINESS_LOGIC_SPECIALIST**: Implement service layer
3. **API_ARCHITECT**: Create API endpoints
4. **FRONTEND_SPECIALIST**: Build user interface
5. **TESTING_SPECIALIST**: Create comprehensive tests
6. **SECURITY_SPECIALIST**: Review security implications

### Performance Optimization
1. **PERFORMANCE_SPECIALIST**: Identify bottlenecks
2. **DATABASE_ARCHITECT**: Optimize queries and indexing
3. **API_ARCHITECT**: Implement response caching
4. **DEVOPS_SPECIALIST**: Configure production optimizations

### Integration Project
1. **INTEGRATION_SPECIALIST**: Design external API connections
2. **ETL_SPECIALIST**: Implement data synchronization
3. **SECURITY_SPECIALIST**: Secure external connections
4. **TESTING_SPECIALIST**: Test integration scenarios

## Tips for Maximum Effectiveness

1. **Start with the Right Sub-Agent**: Choose the specialist most aligned with your primary concern
2. **Provide Project Context**: Sub-agents know your GCC application but benefit from specific context about what you're trying to achieve
3. **Ask for Explanations**: Request explanations of recommendations to understand the reasoning
4. **Request Code Examples**: Ask for specific implementation examples relevant to your project structure
5. **Consider Dependencies**: Think about how changes in one area might affect others

## Sub-Agent Capabilities Summary

Each sub-agent can:
- Analyze existing code in their domain
- Provide specific implementation recommendations
- Create new code following your project patterns
- Debug issues in their expertise area
- Recommend best practices for your specific architecture
- Coordinate with other sub-agents when tasks overlap

The sub-agents are designed to work as your specialized development team, each bringing deep expertise in their domain while understanding your overall GCC Web Application architecture and business requirements.
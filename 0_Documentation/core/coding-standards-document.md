# Coding Standards Document - GCC Database Solution

## Overview
This document defines the coding standards and conventions for the GCC Database Solution ASP.NET Core MVC application. These standards ensure code consistency, maintainability, and readability across the development team.

## General Principles

### Core Values
- **Consistency**: Follow established patterns throughout the codebase
- **Readability**: Write code that tells a story
- **Maintainability**: Design for future developers
- **Security**: Security-first approach in all implementations
- **Performance**: Consider performance implications of coding decisions

### Design Patterns
- **Repository Pattern**: Use for data access layer
- **Service Layer Pattern**: Implement business logic in services
- **MVC Pattern**: Follow ASP.NET Core MVC conventions
- **Dependency Injection**: Use built-in DI container

## C# Coding Standards

### Naming Conventions

#### Classes and Interfaces
```csharp
// Classes: PascalCase
public class CertificateService { }
public class CompanyRepository { }

// Interfaces: PascalCase with 'I' prefix
public interface ICertificateService { }
public interface ICompanyRepository { }

// Abstract classes: PascalCase with 'Base' suffix
public abstract class BaseController { }
```

#### Methods and Properties
```csharp
// Methods: PascalCase, descriptive verbs
public async Task<Certificate> GetCertificateByIdAsync(int id) { }
public bool ValidateCertificateData(Certificate certificate) { }

// Properties: PascalCase, descriptive nouns
public string CompanyName { get; set; }
public DateTime ExpirationDate { get; set; }
public bool IsActive { get; set; }
```

#### Variables and Parameters
```csharp
// Local variables: camelCase
var certificateList = new List<Certificate>();
string companyName = company.Name;

// Parameters: camelCase
public Certificate CreateCertificate(string certificateName, int companyId) { }

// Private fields: camelCase with underscore prefix
private readonly ICertificateService _certificateService;
private readonly ILogger<CertificateController> _logger;
```

#### Constants and Enums
```csharp
// Constants: PascalCase
public const string DefaultConnectionString = "Server=(localdb)\\mssqllocaldb;";
public const int MaxCertificatesPerPage = 50;

// Enums: PascalCase for enum and values
public enum CertificateStatus
{
    Draft,
    UnderReview,
    Approved,
    Rejected,
    Expired
}
```

### Code Structure

#### File Organization
```csharp
// File header (if needed for copyright/license)
// Using statements (grouped and ordered)
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Services;

// Namespace
namespace GCCWebApp.Controllers
{
    // Class definition with XML documentation
    /// <summary>
    /// Handles certificate-related operations for public and authenticated users
    /// </summary>
    public class CertificateController : Controller
    {
        // Private fields
        private readonly ICertificateService _certificateService;
        private readonly ILogger<CertificateController> _logger;

        // Constructor
        public CertificateController(
            ICertificateService certificateService,
            ILogger<CertificateController> logger)
        {
            _certificateService = certificateService;
            _logger = logger;
        }

        // Public methods
        // Private methods
    }
}
```

#### Method Structure
```csharp
/// <summary>
/// Retrieves a certificate by its unique identifier
/// </summary>
/// <param name="id">The certificate ID</param>
/// <returns>Certificate details or NotFound result</returns>
[HttpGet]
public async Task<IActionResult> Details(int id)
{
    // Input validation
    if (id <= 0)
    {
        _logger.LogWarning("Invalid certificate ID provided: {Id}", id);
        return BadRequest("Invalid certificate ID");
    }

    try
    {
        // Business logic
        var certificate = await _certificateService.GetCertificateByIdAsync(id);

        if (certificate == null)
        {
            _logger.LogInformation("Certificate not found with ID: {Id}", id);
            return NotFound();
        }

        // Success response
        return View(certificate);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving certificate with ID: {Id}", ex);
        return StatusCode(500, "An error occurred while retrieving the certificate");
    }
}
```

### Error Handling

#### Exception Handling Pattern
```csharp
// Service Layer Exception Handling
public async Task<Certificate> GetCertificateByIdAsync(int id)
{
    try
    {
        return await _repository.GetByIdAsync(id);
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error retrieving certificate {Id}", id);
        throw new ServiceException("Failed to retrieve certificate", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error retrieving certificate {Id}", id);
        throw;
    }
}

// Controller Exception Handling
[HttpGet]
public async Task<IActionResult> GetCertificate(int id)
{
    try
    {
        var certificate = await _certificateService.GetCertificateByIdAsync(id);
        return Ok(certificate);
    }
    catch (ServiceException ex)
    {
        _logger.LogWarning(ex, "Service error for certificate {Id}", id);
        return StatusCode(500, new { error = "Service temporarily unavailable" });
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Invalid request for certificate {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}
```

#### Logging Standards
```csharp
// Use structured logging with parameters
_logger.LogInformation("Certificate {CertificateId} created for company {CompanyId}",
    certificate.Id, certificate.CompanyId);

// Log levels usage:
// LogTrace: Detailed diagnostic information
// LogDebug: General debugging information
// LogInformation: General application flow
// LogWarning: Unexpected but recoverable situations
// LogError: Error conditions that should be investigated
// LogCritical: Critical failures requiring immediate attention

// Example usage
_logger.LogTrace("Entering GetCertificateByIdAsync with ID: {Id}", id);
_logger.LogDebug("Database query executed successfully");
_logger.LogInformation("Certificate {Id} retrieved successfully", certificate.Id);
_logger.LogWarning("Certificate {Id} is approaching expiration", certificate.Id);
_logger.LogError(ex, "Failed to save certificate {Id}", certificate.Id);
_logger.LogCritical("Database connection failed");
```

### Nullable Reference Types

#### Enable Nullable Context
```csharp
#nullable enable

// Proper nullable annotations
public class Certificate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // Required
    public string? Description { get; set; }          // Optional
    public Company Company { get; set; } = null!;     // Required, set by EF
}
```

#### Null Checking Patterns
```csharp
// Parameter validation
public void ProcessCertificate(Certificate? certificate)
{
    ArgumentNullException.ThrowIfNull(certificate);

    // Or traditional pattern
    if (certificate is null)
        throw new ArgumentNullException(nameof(certificate));
}

// Safe navigation
var companyName = certificate?.Company?.Name ?? "Unknown";

// Pattern matching
if (certificate is { Company.Name: var name } && !string.IsNullOrEmpty(name))
{
    // Use name safely
}
```

## ASP.NET Core Specific Standards

### Controller Standards

#### Controller Structure
```csharp
[Authorize]
[Route("[controller]")]
public class CertificateController : Controller
{
    private readonly ICertificateService _certificateService;
    private readonly IMapper _mapper;
    private readonly ILogger<CertificateController> _logger;

    public CertificateController(
        ICertificateService certificateService,
        IMapper mapper,
        ILogger<CertificateController> logger)
    {
        _certificateService = certificateService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(CertificateSearchRequest request)
    {
        var certificates = await _certificateService.SearchCertificatesAsync(request);
        var viewModel = _mapper.Map<CertificateListViewModel>(certificates);
        return View(viewModel);
    }
}
```

#### Action Method Standards
```csharp
// Use explicit HTTP verb attributes
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateCertificateRequest request)
{
    // Model validation
    if (!ModelState.IsValid)
    {
        return View(request);
    }

    // Business logic
    var result = await _certificateService.CreateCertificateAsync(request);

    if (result.IsSuccess)
    {
        TempData["SuccessMessage"] = "Certificate created successfully";
        return RedirectToAction(nameof(Details), new { id = result.Data.Id });
    }

    // Handle failure
    ModelState.AddModelError("", result.ErrorMessage);
    return View(request);
}
```

### Model Standards

#### Entity Models
```csharp
public class Certificate
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    // Navigation properties
    public int CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<CertificateDocument> Documents { get; set; } = new List<CertificateDocument>();
}
```

#### ViewModels
```csharp
public class CertificateListViewModel
{
    public IEnumerable<CertificateItemViewModel> Certificates { get; set; } = Enumerable.Empty<CertificateItemViewModel>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }
}

public class CertificateItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

#### DTOs and Requests
```csharp
public record CreateCertificateRequest(
    string Name,
    string? Description,
    int CompanyId,
    DateTime? ExpirationDate);

public record CertificateSearchRequest(
    string? SearchTerm = null,
    int? CompanyId = null,
    int PageNumber = 1,
    int PageSize = 20);
```

### Service Layer Standards

#### Service Interface
```csharp
public interface ICertificateService
{
    Task<ServiceResult<Certificate>> CreateCertificateAsync(CreateCertificateRequest request);
    Task<Certificate?> GetCertificateByIdAsync(int id);
    Task<PagedResult<Certificate>> SearchCertificatesAsync(CertificateSearchRequest request);
    Task<ServiceResult> UpdateCertificateAsync(int id, UpdateCertificateRequest request);
    Task<ServiceResult> DeleteCertificateAsync(int id);
}
```

#### Service Implementation
```csharp
public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _repository;
    private readonly IValidator<CreateCertificateRequest> _validator;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        ICertificateRepository repository,
        IValidator<CreateCertificateRequest> validator,
        ILogger<CertificateService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ServiceResult<Certificate>> CreateCertificateAsync(CreateCertificateRequest request)
    {
        // Validation
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return ServiceResult<Certificate>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        try
        {
            // Business logic
            var certificate = new Certificate
            {
                Name = request.Name,
                Description = request.Description,
                CompanyId = request.CompanyId,
                CreatedDate = DateTime.UtcNow,
                ExpirationDate = request.ExpirationDate
            };

            await _repository.AddAsync(certificate);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Certificate {CertificateId} created successfully", certificate.Id);
            return ServiceResult<Certificate>.Success(certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating certificate");
            return ServiceResult<Certificate>.Failure("Failed to create certificate");
        }
    }
}
```

## Database & Entity Framework Standards

### DbContext Configuration
```csharp
public class GCCDbContext : DbContext
{
    public GCCDbContext(DbContextOptions<GCCDbContext> options) : base(options) { }

    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasOne(e => e.Company)
                  .WithMany(c => c.Certificates)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

### Repository Pattern
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<int> SaveChangesAsync();
}

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly GCCDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(GCCDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    // Implementation...
}
```

## Frontend Standards

### Razor Views
```html
@model CertificateListViewModel
@{
    ViewData["Title"] = "Certificates";
}

<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            <h1 class="h2 mb-4">@ViewData["Title"]</h1>

            @if (Model.Certificates.Any())
            {
                <div class="row">
                    @foreach (var certificate in Model.Certificates)
                    {
                        <partial name="_CertificateCard" model="certificate" />
                    }
                </div>
            }
            else
            {
                <div class="alert alert-info">
                    <i class="fas fa-info-circle"></i>
                    No certificates found.
                </div>
            }
        </div>
    </div>
</div>
```

### JavaScript Standards
```javascript
// Use strict mode
'use strict';

// Namespace pattern
const GCC = GCC || {};

GCC.Certificates = {
    // Initialize module
    init: function() {
        this.bindEvents();
        this.loadCertificates();
    },

    // Event binding
    bindEvents: function() {
        $('#searchButton').on('click', this.searchCertificates.bind(this));
        $('#certificateForm').on('submit', this.handleFormSubmit.bind(this));
    },

    // API calls with error handling
    loadCertificates: async function() {
        try {
            const response = await fetch('/api/certificates');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();
            this.renderCertificates(data);
        } catch (error) {
            console.error('Error loading certificates:', error);
            this.showError('Failed to load certificates');
        }
    }
};

// Initialize on DOM ready
$(document).ready(function() {
    GCC.Certificates.init();
});
```

### CSS Standards
```css
/* Use BEM methodology for CSS classes */
.certificate-card {
    border: 1px solid var(--border-color);
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
    margin-bottom: var(--spacing-md);
}

.certificate-card__title {
    font-size: var(--font-size-lg);
    font-weight: var(--font-weight-bold);
    color: var(--primary-color);
}

.certificate-card__meta {
    font-size: var(--font-size-sm);
    color: var(--text-muted);
}

.certificate-card--featured {
    border-color: var(--primary-color);
    background-color: var(--primary-bg-light);
}
```

## Security Standards

### Input Validation
```csharp
// Model validation
public class CreateCertificateRequest
{
    [Required(ErrorMessage = "Certificate name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Valid company ID is required")]
    public int CompanyId { get; set; }
}

// Controller validation
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateCertificateRequest request)
{
    if (!ModelState.IsValid)
    {
        return View(request);
    }
    // Process...
}
```

### Authorization Patterns
```csharp
// Role-based authorization
[Authorize(Roles = "Admin,Analyst")]
public class CertificateManagementController : Controller { }

// Policy-based authorization
[Authorize(Policy = "CanManageCertificates")]
public async Task<IActionResult> Edit(int id) { }

// Resource-based authorization
public async Task<IActionResult> Delete(int id)
{
    var certificate = await _certificateService.GetCertificateByIdAsync(id);
    if (certificate == null) return NotFound();

    var authResult = await _authorizationService.AuthorizeAsync(
        User, certificate, "CanDeleteCertificate");

    if (!authResult.Succeeded)
    {
        return Forbid();
    }
    // Process deletion...
}
```

### SQL Injection Prevention
```csharp
// GOOD: Use parameterized queries with EF Core
var certificates = await _context.Certificates
    .Where(c => c.Name.Contains(searchTerm))
    .ToListAsync();

// GOOD: Use LINQ expressions
var result = await _context.Certificates
    .Where(c => c.CompanyId == companyId && c.IsActive)
    .Select(c => new CertificateDto { Id = c.Id, Name = c.Name })
    .ToListAsync();

// BAD: Never use string concatenation for SQL
// var sql = $"SELECT * FROM Certificates WHERE Name = '{name}'";
```

## Testing Standards

### Unit Testing
```csharp
[Fact]
public async Task GetCertificateByIdAsync_ValidId_ReturnsCertificate()
{
    // Arrange
    var certificate = new Certificate { Id = 1, Name = "Test Certificate" };
    _mockRepository.Setup(r => r.GetByIdAsync(1))
                   .ReturnsAsync(certificate);

    // Act
    var result = await _service.GetCertificateByIdAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Certificate", result.Name);
    _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
}

[Fact]
public async Task GetCertificateByIdAsync_InvalidId_ReturnsNull()
{
    // Arrange
    _mockRepository.Setup(r => r.GetByIdAsync(-1))
                   .ReturnsAsync((Certificate?)null);

    // Act
    var result = await _service.GetCertificateByIdAsync(-1);

    // Assert
    Assert.Null(result);
}
```

### Integration Testing
```csharp
public class CertificateControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CertificateControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Certificates_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Certificate");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8",
            response.Content.Headers.ContentType?.ToString());
    }
}
```

## Performance Standards

### Async/Await Usage
```csharp
// GOOD: Proper async implementation
public async Task<IActionResult> Index()
{
    var certificates = await _certificateService.GetCertificatesAsync();
    return View(certificates);
}

// GOOD: Async all the way down
public async Task<List<Certificate>> GetCertificatesAsync()
{
    return await _context.Certificates.ToListAsync();
}

// BAD: Blocking async calls
// var certificates = _certificateService.GetCertificatesAsync().Result;

// BAD: Unnecessary async
// public async Task<string> GetConstantValue()
// {
//     return "constant";
// }
```

### Entity Framework Performance
```csharp
// GOOD: Use projection for list views
var certificateList = await _context.Certificates
    .Select(c => new CertificateListItem
    {
        Id = c.Id,
        Name = c.Name,
        CompanyName = c.Company.Name
    })
    .ToListAsync();

// GOOD: Use Include for related data
var certificate = await _context.Certificates
    .Include(c => c.Company)
    .Include(c => c.Documents)
    .FirstOrDefaultAsync(c => c.Id == id);

// GOOD: Use pagination
var pagedCertificates = await _context.Certificates
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Code Quality Tools

### Required Tools
1. **StyleCop Analyzers**: Code style enforcement
2. **SonarAnalyzer**: Code quality and security
3. **Microsoft.CodeAnalysis.NetAnalyzers**: .NET specific analysis

### EditorConfig Settings
```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = prefix_interface_with_i

# Code style rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

## Documentation Standards

### XML Documentation
```csharp
/// <summary>
/// Creates a new certificate with the specified details
/// </summary>
/// <param name="request">The certificate creation request containing all required details</param>
/// <returns>A service result containing the created certificate or error information</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null</exception>
/// <exception cref="ValidationException">Thrown when request data is invalid</exception>
public async Task<ServiceResult<Certificate>> CreateCertificateAsync(CreateCertificateRequest request)
{
    // Implementation...
}
```

### README Updates
- Update CLAUDE.md when making significant architectural changes
- Document new patterns or conventions introduced
- Update environment setup instructions when dependencies change

---

## Enforcement and Review

This document should be:
- Reviewed during code reviews
- Updated when new patterns are established
- Referenced during onboarding of new developers
- Used as a checklist for pull request approvals

For questions or clarifications about these standards, create an issue in the project repository.

---
*Last Updated: September 17, 2025*
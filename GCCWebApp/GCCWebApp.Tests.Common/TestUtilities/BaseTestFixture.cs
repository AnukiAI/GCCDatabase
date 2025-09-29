using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GCCWebApp.Data;

namespace GCCWebApp.Tests.Common.TestUtilities;

/// <summary>
/// Base test fixture providing common test infrastructure and utilities
/// </summary>
public abstract class BaseTestFixture : IDisposable
{
    /// <summary>
    /// Gets the web application factory for testing
    /// </summary>
    protected WebApplicationFactory<Program> Factory { get; private set; }

    /// <summary>
    /// Gets the HTTP client for making test requests
    /// </summary>
    protected HttpClient Client { get; private set; }

    /// <summary>
    /// Gets the service scope for dependency injection
    /// </summary>
    protected IServiceScope ServiceScope { get; private set; }

    /// <summary>
    /// Gets the database context for test data operations
    /// </summary>
    protected ApplicationDbContext DbContext { get; private set; }

    /// <summary>
    /// Initializes a new instance of the BaseTestFixture class
    /// </summary>
    protected BaseTestFixture()
    {
        Factory = CreateWebApplicationFactory();
        Client = Factory.CreateClient();
        ServiceScope = Factory.Services.CreateScope();
        DbContext = ServiceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Creates a configured WebApplicationFactory for testing
    /// </summary>
    protected virtual WebApplicationFactory<Program> CreateWebApplicationFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // Configure logging for tests
                    services.AddLogging(builder =>
                    {
                        builder.AddDebug();
                        builder.SetMinimumLevel(LogLevel.Warning);
                    });
                });

                builder.UseEnvironment("Testing");
            });
    }

    /// <summary>
    /// Seeds the test database with common test data
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
        
        // Override in derived classes to add specific test data
        await SeedDefaultTestDataAsync();
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Override this method to provide default test data
    /// </summary>
    protected virtual Task SeedDefaultTestDataAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans the test database
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Disposes of the test resources and cleans up the test environment
    /// </summary>
    public virtual void Dispose()
    {
        DbContext?.Dispose();
        ServiceScope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}
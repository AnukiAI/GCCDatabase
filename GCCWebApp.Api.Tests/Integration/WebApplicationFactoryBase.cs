using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using GCCWebApp;
using GCCWebApp.Data;
using GCCWebApp.Api.Tests.TestUtilities;

namespace GCCWebApp.Api.Tests.Integration
{
    public class WebApplicationFactoryBase<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set environment variables to ensure Testing environment is detected
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            // Set environment early to avoid main Program.cs database initialization
            builder.UseEnvironment("Testing");

            // Override configuration first
            builder.ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnlyAndShouldBeAtLeast256BitsLongForSecurityValidation",
                    ["Jwt:Issuer"] = "GCCWebApp.Api.Tests",
                    ["Jwt:Audience"] = "GCCWebApp.Api.Tests",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["GCC_JWT_SECRET"] = "ThisIsASecretKeyForTestingPurposesOnlyAndShouldBeAtLeast256BitsLongForSecurityValidation",
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:RequestsPerMinute"] = "1000", // Higher limit for testing
                    ["ASPNETCORE_ENVIRONMENT"] = "Testing"
                }!);
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing with unique name
                var dbName = $"IntegrationTestDb_{Guid.NewGuid()}";
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.EnableSensitiveDataLogging();
                });

                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using var scope = serviceProvider.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<WebApplicationFactoryBase<TStartup>>>();

                // Ensure the database is created and seed with test data
                try
                {
                    TestDbContextFactory.SeedTestData(context);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test data.");
                    throw;
                }
            });
        }
    }
}
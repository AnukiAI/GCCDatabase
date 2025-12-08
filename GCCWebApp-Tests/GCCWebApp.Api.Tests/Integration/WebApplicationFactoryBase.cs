using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GCCWebApp;
using GCCWebApp.Data;
using GCCWebApp.Api.Tests.TestUtilities;

namespace GCCWebApp.Api.Tests.Integration
{
    public class WebApplicationFactoryBase<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing - use unique name per test run to avoid conflicts
                var uniqueDbName = $"IntegrationTestDb_{Guid.NewGuid()}";
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(uniqueDbName);
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

            builder.UseEnvironment("Testing");
        }
    }
}
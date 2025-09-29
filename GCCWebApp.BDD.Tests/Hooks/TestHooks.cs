using TechTalk.SpecFlow;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Linq;

namespace GCCWebApp.BDD.Tests.Hooks
{
    [Binding]
    public class TestHooks
    {
        private static WebApplicationFactory<Program>? _factory;
        private static IServiceScope? _scope;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove the existing DbContext registration
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                        
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add DbContext using in-memory database for testing
                        services.AddDbContext<ApplicationDbContext>(options =>
                        {
                            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                        });

                        // Build the service provider
                        var sp = services.BuildServiceProvider();

                        // Create a scope to obtain a reference to the database
                        using var scope = sp.CreateScope();
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                        // Ensure the database is created
                        db.Database.EnsureCreated();

                        // Seed test data if needed
                        SeedTestData(db);
                    });
                });
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            _scope?.Dispose();
            _factory?.Dispose();
        }

        [BeforeScenario("db")]
        public void BeforeScenarioRequiringDatabase(ScenarioContext scenarioContext)
        {
            if (_factory != null)
            {
                _scope = _factory.Services.CreateScope();
                var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                scenarioContext.Set(dbContext);
                
                // Clear data for clean test
                ClearTestData(dbContext);
            }
        }

        [AfterScenario("db")]
        public void AfterScenarioRequiringDatabase()
        {
            _scope?.Dispose();
        }

        [BeforeScenario]
        public void RegisterDependencies(ScenarioContext scenarioContext)
        {
            scenarioContext.Set(_factory);
        }

        private static void SeedTestData(ApplicationDbContext context)
        {
            // Add default test data
            if (!context.CertificationTypes.Any())
            {
                context.CertificationTypes.AddRange(
                    new CertificationType { Name = "ISO 9001", Description = "Quality Management", IsActive = true },
                    new CertificationType { Name = "CE Mark", Description = "European Conformity", IsActive = true },
                    new CertificationType { Name = "FDA Approval", Description = "FDA Certification", IsActive = true },
                    new CertificationType { Name = "ISO 13485", Description = "Medical Devices", IsActive = true }
                );
            }

            if (!context.ProductCategories.Any())
            {
                context.ProductCategories.AddRange(
                    new ProductCategory { Name = "Electronics", Description = "Electronic devices", IsActive = true },
                    new ProductCategory { Name = "Safety", Description = "Safety equipment", IsActive = true },
                    new ProductCategory { Name = "Medical", Description = "Medical devices", IsActive = true }
                );
            }

            context.SaveChanges();
        }

        private void ClearTestData(ApplicationDbContext context)
        {
            // Clear transactional data but keep reference data
            context.Certificates.RemoveRange(context.Certificates);
            context.Products.RemoveRange(context.Products);
            context.Companies.RemoveRange(context.Companies);
            context.SaveChanges();
        }
    }
}
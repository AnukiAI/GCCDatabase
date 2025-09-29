using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using GCCWebApp.Data;
using GCCWebApp.Services;
using Moq;

namespace GCCWebApp.Tests.Integration
{
    public class IntegrationTestBase : IClassFixture<IntegrationTestWebApplicationFactory>, IDisposable
    {
        protected readonly IntegrationTestWebApplicationFactory Factory;
        public readonly HttpClient Client;
        protected readonly IServiceScope Scope;
        public readonly ApplicationDbContext DbContext;

        public IntegrationTestBase(IntegrationTestWebApplicationFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
            Scope = factory.Services.CreateScope();
            DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        protected virtual async Task SeedTestDataAsync()
        {
            // Override in derived classes to add specific test data
            await Task.CompletedTask;
        }

        protected async Task<T> GetServiceAsync<T>() where T : notnull
        {
            return Scope.ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            Scope?.Dispose();
            Client?.Dispose();
        }
    }

    public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Use test configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                    ["Environment"] = "Testing",
                    ["Logging:LogLevel:Default"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                    ["GCC_JWT_SECRET"] = "test-secret-key-for-integration-tests-must-be-very-long-to-meet-requirements"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    options.EnableSensitiveDataLogging();
                });

                // Configure test-specific services
                ConfigureTestServices(services);

                // Build the service provider and ensure database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            });

            builder.UseEnvironment("Testing");
        }

        protected virtual void ConfigureTestServices(IServiceCollection services)
        {
            // Mock external services for integration tests

            // Mock Email Service
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(Task.CompletedTask);
            mockEmailService.Setup(x => x.TestConnectionAsync())
                           .ReturnsAsync(true);

            // Replace the real service with mock
            var emailDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }
            services.AddSingleton(mockEmailService.Object);

            // Mock File Upload Service if it exists
            try
            {
                var fileServiceType = Type.GetType("GCCWebApp.Services.IFileUploadService, GCCWebApp");
                if (fileServiceType != null)
                {
                    var fileDescriptor = services.SingleOrDefault(s => s.ServiceType == fileServiceType);
                    if (fileDescriptor != null)
                    {
                        services.Remove(fileDescriptor);
                    }

                    var mockFileService = new Mock<object>();
                    services.AddSingleton(fileServiceType, mockFileService.Object);
                }
            }
            catch
            {
                // Service doesn't exist, skip
            }

            // Mock Two Factor Service if it exists
            try
            {
                var twoFactorServiceType = Type.GetType("GCCWebApp.Services.ITwoFactorService, GCCWebApp");
                if (twoFactorServiceType != null)
                {
                    var twoFactorDescriptor = services.SingleOrDefault(s => s.ServiceType == twoFactorServiceType);
                    if (twoFactorDescriptor != null)
                    {
                        services.Remove(twoFactorDescriptor);
                    }

                    var mockTwoFactorService = new Mock<object>();
                    services.AddSingleton(twoFactorServiceType, mockTwoFactorService.Object);
                }
            }
            catch
            {
                // Service doesn't exist, skip
            }
        }
    }
}